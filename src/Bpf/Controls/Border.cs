using System;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 装饰容器:带背景、边框、圆角、Padding,包裹单个子控件。
    /// 不是 IPanel(只有一个 Child)。参照 Button 内嵌 TextBlock 的 attach 模式。
    /// </summary>
    public sealed class Border : Control
    {
        private Control? _child;

        // ── 属性 ──

        public static readonly StyledProperty<Brush> BackgroundProperty =
            StyledProperty<Brush>.Register<Border>(nameof(Background),
                new SolidColorBrush(Color.Transparent),
                affectsRender: true);

        public Brush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly StyledProperty<Brush> BorderBrushProperty =
            StyledProperty<Brush>.Register<Border>(nameof(BorderBrush),
                new SolidColorBrush(Color.Transparent),
                affectsRender: true);

        public Brush BorderBrush
        {
            get => GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        /// <summary>边框厚度(四边统一值)。M2.1 简化为 double,后续可扩展为 Thickness。</summary>
        public static readonly StyledProperty<double> BorderThicknessProperty =
            StyledProperty<double>.Register<Border>(nameof(BorderThickness), 0.0,
                affectsMeasure: true, affectsArrange: true);

        public double BorderThickness
        {
            get => GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        /// <summary>圆角半径。</summary>
        public static readonly StyledProperty<double> CornerRadiusProperty =
            StyledProperty<double>.Register<Border>(nameof(CornerRadius), 0.0,
                affectsRender: true);

        public double CornerRadius
        {
            get => GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>内边距(四边统一值)。</summary>
        public static readonly StyledProperty<double> PaddingProperty =
            StyledProperty<double>.Register<Border>(nameof(Padding), 0.0,
                affectsMeasure: true, affectsArrange: true);

        public double Padding
        {
            get => GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        // ── 子控件 ──

        /// <summary>被包裹的子控件(单个,可为 null)。</summary>
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

        protected override void AttachNonPanelChildren(IPlatformWindow window, Control logicalRoot)
        {
            _child?.AttachToHost(window, logicalRoot);
        }

        // ── 布局 ──

        protected override Size MeasureCore(Size availableSize)
        {
            var inset = BorderThickness + Padding;
            if (_child is not null && _child.IsVisible)
            {
                var childAvail = new Size(
                    Math.Max(0, availableSize.Width - inset * 2),
                    Math.Max(0, availableSize.Height - inset * 2));
                _child.Measure(childAvail);
                var desired = _child.DesiredSize;
                return new Size(desired.Width + inset * 2, desired.Height + inset * 2);
            }
            return new Size(inset * 2, inset * 2);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = finalRect;
            if (_child is not null && _child.IsVisible)
            {
                var inset = BorderThickness + Padding;
                var childRect = new Rect(
                    finalRect.X + inset, finalRect.Y + inset,
                    Math.Max(0, finalRect.Width - inset * 2),
                    Math.Max(0, finalRect.Height - inset * 2));
                _child.Arrange(childRect);
            }
        }

        // ── 渲染 ──

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;
            double radius = CornerRadius;
            double bt = BorderThickness;

            // 背景:从 (0,0) 起,用本地尺寸(渲染上下文已被父 push 到 Border.Bounds 原点)
            var localRect = new Rect(0, 0, Bounds.Width, Bounds.Height);
            var bg = Background.ToPlatform(render);
            try
            {
                if (radius > 0)
                    context.FillRoundedRectangle(localRect, radius, radius, bg);
                else
                    context.FillRectangle(localRect, bg);
            }
            finally { bg.Dispose(); }

            // 边框(向内画,不超出 Bounds)
            if (bt > 0)
            {
                var border = BorderBrush.ToPlatform(render);
                try
                {
                    // 边框矩形:从 BorderThickness/2 开始,确保完整边框在 Bounds 内
                    var borderRect = new Rect(
                        bt / 2, bt / 2,
                        Math.Max(0, Bounds.Width - bt),
                        Math.Max(0, Bounds.Height - bt));
                    if (radius > 0)
                        context.DrawRoundedRectangle(borderRect, radius, radius, border, bt);
                    else
                        context.DrawRectangle(borderRect, border, bt);
                }
                finally { border.Dispose(); }
            }

            // 子控件:用相对 Border 原点的偏移(Child.Bounds 是绝对,Bounds 也是绝对,相减得相对)
            if (_child is not null && _child.IsVisible)
            {
                var relX = _child.Bounds.X - Bounds.X;
                var relY = _child.Bounds.Y - Bounds.Y;
                context.PushTranslate(new Vector(relX, relY));
                try
                {
                    _child.Render(context);
                }
                finally
                {
                    context.PopTransform();
                }
            }
        }

        public override bool HitTest(Point point)
        {
            if (!IsVisible || !Bounds.Contains(point)) return false;
            return _child?.HitTest(point) ?? true;
        }
    }
}
