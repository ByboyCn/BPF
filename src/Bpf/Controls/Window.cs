using System;
using System.Collections.Generic;
using Bpf.Input;
using Bpf.Layout;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;
using Bpf.Threading;

namespace Bpf.Controls
{
    /// <summary>
    /// 顶级窗口:逻辑树根,负责协调平台窗口、布局、渲染。
    /// </summary>
    public sealed class Window : Control, IPanel
    {
        private IPlatformWindow? _platformWindow;
        private IRenderTarget? _renderTarget;
        private LayoutManager? _layoutManager;
        private Control _rootPanel;
        // hover 跟踪:派发 PointerMoved 时维护,目标变化时触发 Entered/Exited
        private Control? _lastHoverTarget;
        // 指针捕获:按下时控件可捕获指针,后续 moved/released 都发给捕获者(即使鼠标移出其 Bounds)
        private Control? _capturedControl;

        // ── 标题属性 ──
        public static readonly StyledProperty<string> TitleProperty =
            StyledProperty<string>.Register<Window>(nameof(Title), "bpf Window");

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        // ── 背景属性 ──
        public static readonly StyledProperty<Brush> BackgroundProperty =
            StyledProperty<Brush>.Register<Window>(nameof(Background),
                new SolidColorBrush(Color.White),
                affectsRender: true);

        public Brush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public Window()
        {
            _rootPanel = new Controls.StackPanel { Orientation = Orientation.Vertical };
            _rootPanel.Parent = this;
        }

        /// <summary>窗口内容(根面板)。默认是垂直 StackPanel。</summary>
        public Control Content => _rootPanel;

        /// <summary>替换根面板。若窗口已 attach,会重建 LayoutManager 并把新子树 attach 到 host。</summary>
        public void SetContent(Control panel)
        {
            if (panel is null) throw new ArgumentNullException(nameof(panel));
            _rootPanel = panel;
            panel.Parent = this;

            // 若已 attach,重建 LayoutManager 指向新根,并 attach 新子树
            if (_platformWindow is not null)
            {
                _layoutManager = new LayoutManager(panel);
                panel.AttachToHost(_platformWindow, logicalRoot: this);
                _platformWindow.Invalidate();
            }
        }

        public IReadOnlyList<Control> Children =>
            (_rootPanel as IPanel)?.Children ?? Array.Empty<Control>();

        public void AddChild(Control child)
        {
            if (_rootPanel is IPanel panel) panel.AddChild(child);
            else throw new InvalidOperationException("根面板不支持 AddChild(非 IPanel)。");
        }

        public void RemoveChild(Control child)
        {
            if (_rootPanel is IPanel panel) panel.RemoveChild(child);
        }

        // ── 接入平台窗口 ──

        internal void Attach(IPlatformWindow platformWindow, Dispatcher dispatcher)
        {
            _platformWindow = platformWindow;
            _renderTarget = platformWindow.CreateRenderTarget();
            _layoutManager = new LayoutManager(_rootPanel);

            // 把窗口和 dispatcher 注入到控件树。
            // 注意:Window 自己不 attach(它是逻辑根),而是 attach 它的内容面板 _rootPanel,
            // 由 _rootPanel 的 AttachToHost 递归向下注入整个子树。
            _rootPanel.AttachToHost(platformWindow, logicalRoot: this);

            // 订阅平台事件
            platformWindow.Resized += OnPlatformResized;
            platformWindow.PointerPressed += OnPlatformPointerPressed;
            platformWindow.PointerReleased += OnPlatformPointerReleased;
            platformWindow.PointerMoved += OnPlatformPointerMoved;
            platformWindow.KeyDown += OnPlatformKeyDown;
            platformWindow.KeyUp += OnPlatformKeyUp;
            platformWindow.TextInput += OnPlatformTextInput;
            platformWindow.MouseWheel += OnPlatformMouseWheel;
            platformWindow.Closed += OnPlatformClosed;

            // 应用标题
            platformWindow.Title = Title;
            platformWindow.Show();
            platformWindow.Invalidate();

            // 初始焦点:聚焦第一个可聚焦控件
            FocusManager.TabNext(_rootPanel);
        }

        // ── 键盘派发 ──

        private void OnPlatformKeyDown(object? sender, KeyEventArgs e)
        {
            // Tab 键在窗口层处理:切换焦点(Shift+Tab 反向)
            if (e.Key == Key.Tab)
            {
                if ((e.Modifiers & KeyModifiers.Shift) != 0)
                    FocusManager.TabPrevious(_rootPanel);
                else
                    FocusManager.TabNext(_rootPanel);
                e.Handled = true;
                return;
            }

            // 派发给焦点控件(走 Bubble 路由);无焦点时发给根面板
            var target = FocusManager.Focused ?? (Control)_rootPanel;
            target.RaiseEvent(Control.KeyDownEvent, e);
        }

        private void OnPlatformKeyUp(object? sender, KeyEventArgs e)
        {
            var target = FocusManager.Focused ?? (Control)_rootPanel;
            target.RaiseEvent(Control.KeyUpEvent, e);
        }

        private void OnPlatformTextInput(object? sender, TextEventArgs e)
        {
            var target = FocusManager.Focused ?? (Control)_rootPanel;
            target.RaiseEvent(Control.TextInputEvent, e);
        }

        private void OnPlatformMouseWheel(object? sender, MouseWheelEventArgs e)
        {
            var target = FindHitTestTarget(_rootPanel, e.Position) ?? _rootPanel;
            target.RaiseEvent(Control.MouseWheelEvent, e);
        }

        private void OnPlatformResized(object? sender, SizeChangedEventArgs e)
        {
            _renderTarget?.Resize(e.NewSize);
            _layoutManager?.Invalidate();
            _platformWindow?.Invalidate();
        }

        private void OnPlatformPointerPressed(object? sender, PointerEventArgs e)
        {
            DispatchPointer(e, pressed: true);
        }

        private void OnPlatformPointerReleased(object? sender, PointerEventArgs e)
        {
            DispatchPointer(e, pressed: false);
        }

        private void OnPlatformPointerMoved(object? sender, PointerEventArgs e)
        {
            DispatchPointer(e, moved: true);
        }

        /// <summary>
        /// 递归命中测试:找到命中的最深叶子控件,转换坐标到其本地空间后派发指针事件。
        /// 这修掉了 M1 只遍历 rootPanel.Children 一层的 bug。
        /// </summary>
        private void DispatchPointer(PointerEventArgs e, bool pressed = false, bool moved = false)
        {
            // 指针捕获优先:若有控件捕获了指针,moved/released 都发给它(拖动滑块/滚动条时不丢失)
            Control? target;
            if (_capturedControl is not null && (moved || !pressed))
            {
                target = _capturedControl;
            }
            else
            {
                target = FindHitTestTarget(_rootPanel, e.Position);
                if (target is null)
                {
                    NotifyHoverExit(null, e);
                    return;
                }
                if (moved) NotifyHoverChange(target, e);
            }

            // 把窗口坐标转换为 target 本地坐标(减去所有祖先的 Bounds 原点)
            var localPos = ToLocal(target, e.Position);
            e.Position = localPos;

            if (pressed) target.RaisePointerPressed(e);
            else if (moved) target.RaisePointerMoved(e);
            else
            {
                target.RaisePointerReleased(e);
                // 释放时清除捕获(保险:即使控件忘了 Release)
                if (ReferenceEquals(_capturedControl, target)) _capturedControl = null;
            }
        }

        /// <summary>由 Control.CapturePointer 调用:设置当前指针捕获者。</summary>
        internal void SetPointerCapture(Control control) => _capturedControl = control;
        /// <summary>由 Control.ReleasePointerCapture 调用:清除捕获。</summary>
        internal void ClearPointerCapture(Control control)
        {
            if (ReferenceEquals(_capturedControl, control)) _capturedControl = null;
        }

        /// <summary>hover 目标变化:旧目标离开(置 IsPointerOver=false + OnPointerExited)、新目标进入。</summary>
        private void NotifyHoverChange(Control? newTarget, PointerEventArgs e)
        {
            if (ReferenceEquals(newTarget, _lastHoverTarget)) return;
            // 旧目标离开
            if (_lastHoverTarget is not null)
            {
                _lastHoverTarget.IsPointerOver = false;
                _lastHoverTarget.OnPointerExited(e);
            }
            // 新目标进入
            if (newTarget is not null)
            {
                newTarget.IsPointerOver = true;
                newTarget.OnPointerEntered(e);
            }
            _lastHoverTarget = newTarget;
        }

        /// <summary>指针完全离开窗口(或无命中目标):通知当前 hover 目标退出。</summary>
        private void NotifyHoverExit(Control? newTarget, PointerEventArgs e)
        {
            if (_lastHoverTarget is not null)
            {
                _lastHoverTarget.IsPointerOver = false;
                _lastHoverTarget.OnPointerExited(e);
                _lastHoverTarget = null;
            }
        }

        /// <summary>
        /// 递归找最深命中的控件。point 始终是窗口坐标(不转换)。
        /// 所有控件的 Bounds 经 ArrangeCore 统一在父坐标系,而每层 ArrangeCore 都加上 finalRect
        /// 原点,因此嵌套面板的子控件 Bounds 实际都在窗口坐标系 —— point 和 Bounds 可直接比较。
        /// </summary>
        private static Control? FindHitTestTarget(Control control, Point point)
        {
            if (!control.IsVisible) return null;
            if (!control.Bounds.Contains(point)) return null;

            // 命中本控件。优先深入子控件找更深的命中者。

            // 多子容器(IPanel):遍历 children,RenderOnTop 的优先(在上层)
            if (control is IPanel panel)
            {
                // 先测 RenderOnTop 的(展开的 ComboBox 等)
                for (int i = panel.Children.Count - 1; i >= 0; i--)
                {
                    var child = panel.Children[i];
                    if (!child.RenderOnTop) continue;
                    if (!child.Bounds.Contains(point)) continue;
                    var hit = FindHitTestTarget(child, point);
                    if (hit is not null) return hit;
                    return child;
                }
                // 再测普通的
                for (int i = panel.Children.Count - 1; i >= 0; i--)
                {
                    var child = panel.Children[i];
                    if (child.RenderOnTop) continue;
                    if (!child.Bounds.Contains(point)) continue;
                    var hit = FindHitTestTarget(child, point);
                    if (hit is not null) return hit;
                    return child;
                }
            }

            // 单子装饰器(IContentHost:ScrollViewer/Border):递归进内部 child
            if (control is IContentHost contentHost && contentHost.ContentChild is Control innerChild)
            {
                if (innerChild.Bounds.Contains(point))
                {
                    var hit = FindHitTestTarget(innerChild, point);
                    if (hit is not null) return hit;
                }
                // child 未命中或无更深层,返回装饰器本身
                return control;
            }

            // 叶子控件,或容器自身命中区(无子命中)
            return control;
        }

        /// <summary>把窗口坐标系下的 point 转换为 target 本地坐标(减去祖先链的 Bounds 原点)。</summary>
        /// <summary>把窗口坐标 point 转为 target 本地坐标。
        /// 所有 Bounds 在窗口坐标系(ArrangeCore 统一加 finalRect 原点),
        /// 所以 local = point - target.Bounds.Origin。</summary>
        private static Point ToLocal(Control target, Point point)
        {
            return new Point(point.X - target.Bounds.X, point.Y - target.Bounds.Y);
        }

        private void OnPlatformClosed(object? sender, EventArgs e)
        {
            // 触发应用退出(M1 由 dispatcher 主循环退出实现)
        }

        // ── 布局 & 渲染入口 ──

        protected override void OnPropertyChanged<TValue>(
            StyledProperty<TValue> property, object? oldValue, TValue newValue)
        {
            base.OnPropertyChanged(property, oldValue, newValue);

            // Title 变更同步到原生窗口
            if (ReferenceEquals(property, TitleProperty) && _platformWindow is not null)
            {
                _platformWindow.Title = Title;
            }
        }

        /// <summary>由 dispatcher 在每一帧渲染前调用。</summary>
        internal void RenderFrame()
        {
            if (_platformWindow is null || _renderTarget is null) return;

            // 推进全局 tick(光标闪烁、动画等)。在布局/渲染前,使失效能在本帧体现。
            Bpf.Threading.TickRegistry.TickAll();

            var size = _platformWindow.ClientSize;
            // M4.1:每帧重排队布局(ExecuteLayout 内部有缓存,仅在尺寸/失效时才真正重算)。
            // 这样控件运行时变化(如 ComboBox 展开、属性 AffectsMeasure)能立即触发重布局。
            _layoutManager?.Invalidate();
            _layoutManager?.ExecuteLayout(size);

            using var ctx = _renderTarget.BeginDraw();
            try
            {
                ctx.Clear((Background as SolidColorBrush)?.Color ?? Color.White);
                _rootPanel.Render(ctx);
            }
            finally
            {
                ctx.Dispose();
            }
            _renderTarget.Present();
        }

        // ── 自身布局/渲染:Window 自己不参与 measure/arrange,把活全交给根面板 ──

        protected override Size MeasureCore(Size availableSize) => availableSize;
        protected override void ArrangeCore(Rect finalRect) => Bounds = finalRect;

        public override void Render(Platform.IDrawingContext context) => _rootPanel.Render(context);
    }
}
