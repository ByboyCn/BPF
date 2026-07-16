using System;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 分组框:带标题文字的边框容器,把相关控件组织在一起。
    /// 单子容器(Child),标题画在顶部边框上(经典 Windows 风格)。
    /// 边框留出顶部空间放标题,子控件排在边框内(留内边距)。
    /// </summary>
    public sealed class GroupBox : Control, IContentHost
    {
        private Control? _child;
        private IPlatformTextFormat? _format;

        public static readonly StyledProperty<string> HeaderProperty =
            StyledProperty<string>.Register<GroupBox>(nameof(Header), "",
                affectsMeasure: true, affectsRender: true);
        public string Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value ?? "");
        }

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<GroupBox>(nameof(Foreground),
                new SolidColorBrush(Color.Black), affectsRender: true);
        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<Brush> BorderBrushProperty =
            StyledProperty<Brush>.Register<GroupBox>(nameof(BorderBrush),
                new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), affectsRender: true);
        public Brush BorderBrush
        {
            get => GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public static readonly StyledProperty<string> FontFamilyProperty =
            StyledProperty<string>.Register<GroupBox>(nameof(FontFamily), "Segoe UI",
                affectsMeasure: true, affectsRender: true);
        public string FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly StyledProperty<double> FontSizeProperty =
            StyledProperty<double>.Register<GroupBox>(nameof(FontSize), 14.0,
                affectsMeasure: true, affectsRender: true);
        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>子控件(单个)。</summary>
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

        // 布局常量
        private const double HeaderPad = 8;     // 标题左右内边距
        private const double BorderPad = 6;      // 边框到内容的内边距
        private const double HeaderGap = 4;      // 标题文字到边框顶部的距离

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

        private double HeaderHeight => FontSize + HeaderGap * 2;

        protected override Size MeasureCore(Size availableSize)
        {
            var fmt = EnsureFormat();
            var header = string.IsNullOrEmpty(Header) ? Size.Empty : fmt.MeasureText(Header);

            if (_child is null || !_child.IsVisible)
            {
                double h = string.IsNullOrEmpty(Header) ? BorderPad * 2 : HeaderHeight + BorderPad;
                double w = string.IsNullOrEmpty(Header) ? 0 : header.Width + HeaderPad * 2;
                return new Size(w, h);
            }

            // 子控件在边框内(减去标题高度 + 内边距)
            double availW = availableSize.Width == double.PositiveInfinity ? double.PositiveInfinity : availableSize.Width - BorderPad * 2;
            double availH = availableSize.Height == double.PositiveInfinity ? double.PositiveInfinity : availableSize.Height - HeaderHeight - BorderPad;
            _child.Measure(new Size(Math.Max(0, availW), Math.Max(0, availH)));

            double childW = _child.DesiredSize.Width;
            double childH = _child.DesiredSize.Height;
            double totalW = childW + BorderPad * 2;
            double totalH = childH + HeaderHeight + BorderPad;
            if (!string.IsNullOrEmpty(Header))
                totalW = Math.Max(totalW, header.Width + HeaderPad * 2);
            return new Size(totalW, totalH);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = finalRect;
            if (_child is null || !_child.IsVisible) return;
            // 子控件排在边框内:留出 HeaderHeight 顶部 + BorderPad 四周
            _child.Arrange(new Rect(
                finalRect.X + BorderPad,
                finalRect.Y + HeaderHeight,
                Math.Max(0, finalRect.Width - BorderPad * 2),
                Math.Max(0, finalRect.Height - HeaderHeight - BorderPad)));
        }

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;
            var border = BorderBrush.ToPlatform(render);
            var fmt = EnsureFormat();
            try
            {
                // 边框:顶部留缺口给标题。简化为完整矩形边框 + 标题背景遮挡顶部线。
                context.DrawRectangle(new Rect(0.5, HeaderHeight / 2.0, Bounds.Width - 1, Bounds.Height - HeaderHeight / 2.0 - 0.5),
                    border, 1.0);

                // 标题:画在边框顶部线上,用背景色遮挡边框线(经典风格)
                if (!string.IsNullOrEmpty(Header))
                {
                    var headerSize = fmt.MeasureText(Header);
                    double tx = HeaderPad;
                    double ty = 0; // 标题顶部对齐边框顶
                    var fg = Foreground.ToPlatform(render);
                    try { context.DrawText(new Point(tx, ty), Header, fmt, fg); }
                    finally { fg.Dispose(); }
                }
            }
            finally { border.Dispose(); }

            // 子控件
            if (_child is not null && _child.IsVisible)
            {
                var relX = _child.Bounds.X - Bounds.X;
                var relY = _child.Bounds.Y - Bounds.Y;
                context.PushTranslate(new Vector(relX, relY));
                try { _child.Render(context); }
                finally { context.PopTransform(); }
            }
        }

        public override bool HitTest(Point point)
        {
            if (!IsVisible || !Bounds.Contains(point)) return false;
            return _child?.HitTest(point) ?? true;
        }
    }
}
