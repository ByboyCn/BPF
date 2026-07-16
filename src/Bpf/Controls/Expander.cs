using System;
using Bpf.Controls.Routing;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 折叠面板:点击标题栏展开/收起内容。常见于设置页、侧边栏。
    /// IsExpanded=true 时显示 Child;false 时只显示标题栏(高度收起)。
    /// 标题栏左侧画 ▶/▼ 指示展开状态。
    /// </summary>
    public sealed class Expander : Control, IContentHost
    {
        private Control? _child;
        private IPlatformTextFormat? _format;
        private bool _isExpanded = true;
        private bool _isHeaderHovered;

        public static readonly StyledProperty<string> HeaderProperty =
            StyledProperty<string>.Register<Expander>(nameof(Header), "",
                affectsMeasure: true, affectsRender: true);
        public string Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value ?? "");
        }

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<Expander>(nameof(Foreground),
                new SolidColorBrush(Color.Black), affectsRender: true);
        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<Brush> HeaderBackgroundProperty =
            StyledProperty<Brush>.Register<Expander>(nameof(HeaderBackground),
                new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)), affectsRender: true);
        public Brush HeaderBackground
        {
            get => GetValue(HeaderBackgroundProperty);
            set => SetValue(HeaderBackgroundProperty, value);
        }

        public static readonly StyledProperty<Brush> BorderBrushProperty =
            StyledProperty<Brush>.Register<Expander>(nameof(BorderBrush),
                new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), affectsRender: true);
        public Brush BorderBrush
        {
            get => GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public static readonly StyledProperty<string> FontFamilyProperty =
            StyledProperty<string>.Register<Expander>(nameof(FontFamily), "Segoe UI",
                affectsMeasure: true, affectsRender: true);
        public string FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly StyledProperty<double> FontSizeProperty =
            StyledProperty<double>.Register<Expander>(nameof(FontSize), 14.0,
                affectsMeasure: true, affectsRender: true);
        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>是否展开(显示内容)。</summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    InvalidateMeasure();
                    RaiseEvent(ExpandedChangedEvent, new RoutedEventArgs());
                }
            }
        }

        // 路由事件:展开状态变化
        public static readonly RoutedEvent<RoutedEventArgs> ExpandedChangedEvent =
            RoutedEvent<RoutedEventArgs>.Register<Expander>(nameof(ExpandedChanged), RoutingStrategies.Bubble);
        public event EventHandler<RoutedEventArgs>? ExpandedChanged
        {
            add => AddHandler(ExpandedChangedEvent, value!);
            remove => RemoveHandler(ExpandedChangedEvent, value!);
        }

        /// <summary>子控件(展开时显示)。</summary>
        public Control? Child
        {
            get => _child;
            set
            {
                if (_child is not null) _child.Parent = null;
                _child = value;
                if (_child is not null)
                {
                    _child.Parent = this;
                    if (HostWindow is not null)
                        _child.AttachToHost(HostWindow, logicalRoot: LogicalRoot);
                }
                InvalidateMeasure();
            }
        }

        Control? IContentHost.ContentChild => _child;

        private const double HeaderPad = 8;
        private const double HeaderH = 28;
        private const double BorderPad = 6;

        protected override void OnAttachedToHost()
        {
            base.OnAttachedToHost();
            RebuildFormat();
        }

        protected override void AttachNonPanelChildren(IPlatformWindow window, Control logicalRoot)
        {
            _child?.AttachToHost(window, logicalRoot);
        }

        private void RebuildFormat()
        {
            if (HostWindow is null) return;
            _format?.Dispose();
            _format = Bpf.Application.Application.Current.RenderInterface.CreateTextFormat(
                FontFamily, FontSize, FontWeight.Normal);
        }

        private IPlatformTextFormat EnsureFormat()
        {
            if (_format is null) RebuildFormat();
            return _format!;
        }

        protected override Size MeasureCore(Size availableSize)
        {
            var fmt = EnsureFormat();
            if (!_isExpanded || _child is null || !_child.IsVisible)
            {
                // 收起:只占标题栏高度
                double w = availableSize.Width == double.PositiveInfinity ? 120 : availableSize.Width;
                return new Size(w, HeaderH);
            }

            double availW = availableSize.Width == double.PositiveInfinity ? double.PositiveInfinity : availableSize.Width - BorderPad * 2;
            double availH = availableSize.Height == double.PositiveInfinity ? double.PositiveInfinity : availableSize.Height - HeaderH - BorderPad;
            _child.Measure(new Size(Math.Max(0, availW), Math.Max(0, availH)));

            double w2 = availableSize.Width == double.PositiveInfinity ? _child.DesiredSize.Width + BorderPad * 2 : availableSize.Width;
            double h2 = HeaderH + _child.DesiredSize.Height + BorderPad;
            return new Size(w2, h2);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = finalRect;
            if (!_isExpanded || _child is null || !_child.IsVisible) return;
            _child.Arrange(new Rect(
                finalRect.X + BorderPad,
                finalRect.Y + HeaderH,
                Math.Max(0, finalRect.Width - BorderPad * 2),
                Math.Max(0, finalRect.Height - HeaderH - BorderPad)));
        }

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;
            var fmt = EnsureFormat();

            // 标题栏背景(悬停时略深)
            var headerBg = (_isHeaderHovered
                ? new SolidColorBrush(Bpf.Theming.Theme.Current.HoverBackground)
                : HeaderBackground).ToPlatform(render);
            try { context.FillRectangle(new Rect(0, 0, Bounds.Width, HeaderH), headerBg); }
            finally { headerBg.Dispose(); }

            // 展开指示符:自绘三角形(避免 Unicode ▶▼ 缺字显示为方块)。
            // 收起:▶ 朝右;展开:▼ 朝下。三角形画在标题栏左侧,垂直居中。
            var arrow = Foreground.ToPlatform(render);
            try
            {
                double cx = 10, cy = HeaderH / 2.0, s = 4; // 三角中心 + 半尺寸
                if (_isExpanded)
                {
                    // 朝下三角 ▼
                    context.FillTriangle(new Point(cx - s, cy - s / 2), new Point(cx + s, cy - s / 2), new Point(cx, cy + s), arrow);
                }
                else
                {
                    // 朝右三角 ▶
                    context.FillTriangle(new Point(cx - s / 2, cy - s), new Point(cx - s / 2, cy + s), new Point(cx + s, cy), arrow);
                }
            }
            finally { arrow.Dispose(); }

            // 标题文字
            if (!string.IsNullOrEmpty(Header))
            {
                var fg = Foreground.ToPlatform(render);
                try { context.DrawText(new Point(22, (HeaderH - FontSize) / 2.0 - 1), Header, fmt, fg); }
                finally { fg.Dispose(); }
            }

            // 边框(展开时画完整边框;收起时只画标题栏底线)
            var border = BorderBrush.ToPlatform(render);
            try
            {
                if (_isExpanded)
                {
                    context.DrawRectangle(new Rect(0.5, 0.5, Bounds.Width - 1, Bounds.Height - 1), border, 1.0);
                }
                else
                {
                    context.DrawRectangle(new Rect(0, HeaderH - 0.5, Bounds.Width, 1), border, 1.0);
                }
            }
            finally { border.Dispose(); }

            // 子控件
            if (_isExpanded && _child is not null && _child.IsVisible)
            {
                var relX = _child.Bounds.X - Bounds.X;
                var relY = _child.Bounds.Y - Bounds.Y;
                context.PushTranslate(new Vector(relX, relY));
                try { _child.Render(context); }
                finally { context.PopTransform(); }
            }
        }

        // ── 输入:点击标题栏切换展开 ──

        public override void OnPointerPressed(PointerEventArgs e)
        {
            if (e.Position.Y <= HeaderH)
            {
                IsExpanded = !IsExpanded;
                e.Handled = true;
                return;
            }
            // 点击内容区:透传给子控件(不设 Handled)
        }

        protected internal override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            _isHeaderHovered = e.Position.Y <= HeaderH;
            InvalidateVisual();
        }

        public override void OnPointerMoved(PointerEventArgs e)
        {
            // 标题栏悬停高亮(鼠标在标题栏内移动时)
            bool inHeader = e.Position.Y <= HeaderH;
            if (inHeader != _isHeaderHovered)
            {
                _isHeaderHovered = inHeader;
                InvalidateVisual();
            }
        }

        protected internal override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            _isHeaderHovered = false;
            InvalidateVisual();
        }

        public override bool HitTest(Point point)
        {
            if (!IsVisible || !Bounds.Contains(point)) return false;
            if (point.Y <= HeaderH) return true; // 标题栏:本控件处理
            return _child?.HitTest(point) ?? false;
        }
    }
}
