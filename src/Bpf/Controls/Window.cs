using System;
using System.Collections.Generic;
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
            platformWindow.Closed += OnPlatformClosed;

            // 应用标题
            platformWindow.Title = Title;
            platformWindow.Show();
            platformWindow.Invalidate();
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

        /// <summary>M1 命中测试:从顶层向下,找到第一个命中的控件并派发事件。</summary>
        private void DispatchPointer(PointerEventArgs e, bool pressed = false, bool moved = false)
        {
            // 用根面板做命中测试,转发给命中的子控件
            if (_rootPanel.HitTest(e.Position))
            {
                // 简化:遍历 children 找命中的
                foreach (var child in _rootPanel.Children)
                {
                    if (child.HitTest(e.Position))
                    {
                        if (pressed) child.RaisePointerPressed(e);
                        else if (moved) child.RaisePointerMoved(e);
                        else child.RaisePointerReleased(e);
                        break;
                    }
                }
            }
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
