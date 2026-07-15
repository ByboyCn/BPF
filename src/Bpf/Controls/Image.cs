using System;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 图片控件(M4.1 占位)。
    /// 真正的图片加载(WIC + D2D1 DrawBitmap)留 M5。
    /// 当前用纯色矩形渲染占位,不崩溃。
    /// </summary>
    public sealed class Image : Control
    {
        /// <summary>图片源(M4.1 未使用,纯占位)。</summary>
        public object? Source { get; set; }

        public static readonly StyledProperty<double> WidthProperty =
            StyledProperty<double>.Register<Image>(nameof(Width), 64.0,
                affectsMeasure: true);

        public double Width
        {
            get => GetValue(WidthProperty);
            set => SetValue(WidthProperty, value);
        }

        public static readonly StyledProperty<double> HeightProperty =
            StyledProperty<double>.Register<Image>(nameof(Height), 64.0,
                affectsMeasure: true);

        public double Height
        {
            get => GetValue(HeightProperty);
            set => SetValue(HeightProperty, value);
        }

        public static readonly StyledProperty<Brush> PlaceholderBrushProperty =
            StyledProperty<Brush>.Register<Image>(nameof(PlaceholderBrush),
                new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)), affectsRender: true);

        public Brush PlaceholderBrush
        {
            get => GetValue(PlaceholderBrushProperty);
            set => SetValue(PlaceholderBrushProperty, value);
        }

        protected override Size MeasureCore(Size availableSize)
        {
            double w = Math.Min(availableSize.Width, Width);
            double h = Math.Min(availableSize.Height, Height);
            return new Size(w, h);
        }

        protected override void ArrangeCore(Rect finalRect) => Bounds = finalRect;

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;
            var brush = PlaceholderBrush.ToPlatform(render);
            try
            {
                // 占位:纯色矩形 + 对角线(用两条细矩形近似)
                context.FillRectangle(new Rect(0, 0, Bounds.Width, Bounds.Height), brush);
                var border = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)).ToPlatform(render);
                try
                {
                    context.DrawRectangle(new Rect(0.5, 0.5, Bounds.Width - 1, Bounds.Height - 1), border, 1.0);
                }
                finally { border.Dispose(); }
            }
            finally { brush.Dispose(); }
        }

        public override bool HitTest(Point point) =>
            IsVisible && Bounds.Contains(point);
    }
}
