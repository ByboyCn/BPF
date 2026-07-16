using System;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;
using Bpf.Threading;

namespace Bpf.Controls
{
    /// <summary>
    /// 进度条。两种模式:
    /// - 确定(IsIndeterminate=false):Value/Minimum/Maximum 决定填充比例(0..100%)。
    /// - 不确定(IsIndeterminate=true):显示来回滚动的动画块,表示"进行中但未知进度"。
    /// 默认高度 16,圆角,带主题色填充。
    /// </summary>
    public sealed class ProgressBar : Control, ITickable
    {
        public static readonly StyledProperty<double> MinimumProperty =
            StyledProperty<double>.Register<ProgressBar>(nameof(Minimum), 0.0, affectsRender: true);
        public double Minimum
        {
            get => GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly StyledProperty<double> MaximumProperty =
            StyledProperty<double>.Register<ProgressBar>(nameof(Maximum), 100.0, affectsRender: true);
        public double Maximum
        {
            get => GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly StyledProperty<double> ValueProperty =
            StyledProperty<double>.Register<ProgressBar>(nameof(Value), 0.0, affectsRender: true);
        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, Math.Clamp(value, Minimum, Maximum));
        }

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<ProgressBar>(nameof(Foreground),
                new SolidColorBrush(Bpf.Theming.Theme.Current.Accent), affectsRender: true);
        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<Brush> TrackBrushProperty =
            StyledProperty<Brush>.Register<ProgressBar>(nameof(TrackBrush),
                new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)), affectsRender: true);
        public Brush TrackBrush
        {
            get => GetValue(TrackBrushProperty);
            set => SetValue(TrackBrushProperty, value);
        }

        /// <summary>不确定模式:显示来回滚动的动画块。</summary>
        public bool IsIndeterminate { get; set; }

        // 不确定模式的动画相位(0..1)
        private double _indeterminatePhase;

        public ProgressBar() { }

        protected override void OnAttachedToHost()
        {
            base.OnAttachedToHost();
            if (IsIndeterminate) TickRegistry.Register(this);
        }

        bool ITickable.Tick(double dt)
        {
            if (!IsIndeterminate) return false;
            _indeterminatePhase += dt * 0.8; // 速度
            if (_indeterminatePhase > 1.0) _indeterminatePhase -= 2.0; // 来回(0→1→-1→0 循环)
            InvalidateVisual();
            return IsIndeterminate;
        }

        protected override Size MeasureCore(Size availableSize)
        {
            double h = 16; // 默认高度
            double w = availableSize.Width == double.PositiveInfinity ? 120 : availableSize.Width;
            return new Size(w, h);
        }

        protected override void ArrangeCore(Rect finalRect) => Bounds = finalRect;

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;
            var track = TrackBrush.ToPlatform(render);
            var fill = Foreground.ToPlatform(render);
            try
            {
                double radius = Bounds.Height / 2.0;
                // 轨道(圆角背景)
                context.FillRoundedRectangle(new Rect(0, 0, Bounds.Width, Bounds.Height), radius, radius, track);

                if (IsIndeterminate)
                {
                    // 不确定:一个宽度 40% 的块在轨道上来回移动
                    double blockW = Bounds.Width * 0.4;
                    // phase: 0..1 时从左到右,-1..0 时从右到左
                    double t = Math.Abs(_indeterminatePhase);
                    double blockX = t * (Bounds.Width - blockW);
                    context.FillRoundedRectangle(new Rect(blockX, 0, blockW, Bounds.Height), radius, radius, fill);
                }
                else
                {
                    // 确定:按比例填充
                    double range = Maximum - Minimum;
                    if (range <= 0) return;
                    double ratio = Math.Clamp((Value - Minimum) / range, 0, 1);
                    double fillW = Bounds.Width * ratio;
                    if (fillW > 0.5)
                        context.FillRoundedRectangle(new Rect(0, 0, fillW, Bounds.Height), radius, radius, fill);
                }
            }
            finally { track.Dispose(); fill.Dispose(); }
        }

        public override bool HitTest(Point point) => false; // 进度条不接受点击
    }
}
