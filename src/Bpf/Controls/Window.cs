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
        private readonly Controls.StackPanel _rootPanel;

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

        /// <summary>窗口内容(根面板)。</summary>
        public Controls.StackPanel Content => _rootPanel;

        public IReadOnlyList<Control> Children => _rootPanel.Children;

        public void AddChild(Control child) => _rootPanel.AddChild(child);
        public void RemoveChild(Control child) => _rootPanel.RemoveChild(child);

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
            var target = FindHitTestTarget(_rootPanel, e.Position);
            if (target is null) return;

            // 把窗口坐标转换为 target 本地坐标(减去所有祖先的 Bounds 原点)
            var localPos = ToLocal(target, e.Position);
            e.Position = localPos;

            if (pressed) target.RaisePointerPressed(e);
            else if (moved) target.RaisePointerMoved(e);
            else target.RaisePointerReleased(e);
        }

        /// <summary>
        /// 递归找最深命中的控件。point 始终在 control 的父坐标系(对 root 是窗口坐标)。
        /// 关键:child.Bounds 也在同一父坐标系,直接用 Contains 判断,不要做坐标转换。
        /// 只有进入子控件内部递归时,才把 point 转换到子控件的父坐标系(即减去 child.Bounds 原点)。
        /// </summary>
        private static Control? FindHitTestTarget(Control control, Point point)
        {
            if (!control.IsVisible) return null;
            if (!control.Bounds.Contains(point)) return null;

            // 命中本控件。优先深入子控件找更深的命中者。
            if (control is IPanel panel)
            {
                // 倒序遍历:后绘制的在上层,优先命中
                for (int i = panel.Children.Count - 1; i >= 0; i--)
                {
                    var child = panel.Children[i];
                    // child.Bounds 在 control 的坐标系里,point 也在,直接判断
                    if (!child.Bounds.Contains(point)) continue;
                    // 命中 child:转换到 child 的坐标系(减去 child.Bounds 原点),递归
                    var childLocal = new Point(point.X - child.Bounds.X, point.Y - child.Bounds.Y);
                    var hit = FindHitTestTarget(child, childLocal);
                    if (hit is not null) return hit;
                    // child 内部未命中更深的,但 child 自身命中 —— 返回 child
                    return child;
                }
            }

            // 叶子控件,或容器自身命中区(无子命中)
            return control;
        }

        /// <summary>把窗口坐标系下的 point 转换为 target 本地坐标(减去祖先链的 Bounds 原点)。</summary>
        private static Point ToLocal(Control target, Point point)
        {
            double x = point.X, y = point.Y;
            Control? c = target;
            while (c is not null)
            {
                x -= c.Bounds.X;
                y -= c.Bounds.Y;
                c = c.Parent;
            }
            return new Point(x, y);
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

            var size = _platformWindow.ClientSize;
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
