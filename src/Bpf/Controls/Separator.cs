using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 分隔线。水平或垂直方向的细线,用于分隔 UI 区域。
    /// Orientation=Horizontal 时画水平线(占用少量高度,占满父宽度);
    /// Orientation=Vertical 时画垂直线(占用少量宽度,占满父高度)。
    /// </summary>
    public sealed class Separator : Control
    {
        public static readonly StyledProperty<Brush> SeparatorBrushProperty =
            StyledProperty<Brush>.Register<Separator>(nameof(SeparatorBrush),
                new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), affectsRender: true);

        public Brush SeparatorBrush
        {
            get => GetValue(SeparatorBrushProperty);
            set => SetValue(SeparatorBrushProperty, value);
        }

        /// <summary>线粗细(像素)。默认 1。</summary>
        public static readonly StyledProperty<double> ThicknessProperty =
            StyledProperty<double>.Register<Separator>(nameof(Thickness), 1.0, affectsMeasure: true);

        public double Thickness
        {
            get => GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        /// <summary>方向。水平(默认)或垂直。</summary>
        public Orientation Orientation { get; set; } = Orientation.Horizontal;

        protected override Size MeasureCore(Size availableSize)
        {
            return Orientation == Orientation.Horizontal
                ? new Size(0, Thickness)   // 水平:高度=粗细,宽度由父给
                : new Size(Thickness, 0);  // 垂直:宽度=粗细,高度由父给
        }

        protected override void ArrangeCore(Rect finalRect) => Bounds = finalRect;

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;
            var brush = SeparatorBrush.ToPlatform(render);
            try
            {
                if (Orientation == Orientation.Horizontal)
                {
                    // 水平线:居中画一条 Thickness 高的矩形,占满宽度
                    double y = (Bounds.Height - Thickness) / 2.0;
                    context.FillRectangle(new Rect(0, y, Bounds.Width, Thickness), brush);
                }
                else
                {
                    double x = (Bounds.Width - Thickness) / 2.0;
                    context.FillRectangle(new Rect(x, 0, Thickness, Bounds.Height), brush);
                }
            }
            finally { brush.Dispose(); }
        }

        public override bool HitTest(Point point) => false; // 分隔线不接受点击
    }
}
