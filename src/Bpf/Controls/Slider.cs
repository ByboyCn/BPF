using System;
using Bpf.Controls.Routing;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 滑动条。点击/拖拽轨道调节 Value(Minimum~Maximum)。
    /// </summary>
    public sealed class Slider : Control
    {
        // ── 属性 ──

        public static readonly StyledProperty<double> MinimumProperty =
            StyledProperty<double>.Register<Slider>(nameof(Minimum), 0.0, affectsArrange: true);

        public double Minimum
        {
            get => GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly StyledProperty<double> MaximumProperty =
            StyledProperty<double>.Register<Slider>(nameof(Maximum), 100.0, affectsArrange: true);

        public double Maximum
        {
            get => GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly StyledProperty<double> ValueProperty =
            StyledProperty<double>.Register<Slider>(nameof(Value), 0.0, affectsRender: true);

        public double Value
        {
            get => GetValue(ValueProperty);
            set
            {
                var clamped = Math.Clamp(value, Minimum, Maximum);
                SetValue(ValueProperty, clamped);
                RaiseEvent(ValueChangedEvent, new RoutedEventArgs());
                InvalidateVisual();
            }
        }

        public static readonly StyledProperty<Brush> TrackBrushProperty =
            StyledProperty<Brush>.Register<Slider>(nameof(TrackBrush),
                new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), affectsRender: true);

        public Brush TrackBrush
        {
            get => GetValue(TrackBrushProperty);
            set => SetValue(TrackBrushProperty, value);
        }

        public static readonly StyledProperty<Brush> ThumbBrushProperty =
            StyledProperty<Brush>.Register<Slider>(nameof(ThumbBrush),
                new SolidColorBrush(Color.FromRgb(0x00, 0x70, 0xC0)), affectsRender: true);

        public Brush ThumbBrush
        {
            get => GetValue(ThumbBrushProperty);
            set => SetValue(ThumbBrushProperty, value);
        }

        // ── 路由事件 ──

        public static readonly RoutedEvent<RoutedEventArgs> ValueChangedEvent =
            RoutedEvent<RoutedEventArgs>.Register<Slider>(nameof(ValueChanged), RoutingStrategies.Bubble);

        public event EventHandler<RoutedEventArgs>? ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value!);
            remove => RemoveHandler(ValueChangedEvent, value!);
        }

        // ── 拖拽状态 ──

        private bool _isDragging;

        public Slider()
        {
            IsFocusable = true;
        }

        // ── 布局 ──

        private const double DefaultWidth = 150;
        private const double DefaultHeight = 24;
        private const double ThumbWidth = 10;
        private const double TrackHeight = 4;

        protected override Size MeasureCore(Size availableSize) =>
            new Size(
                availableSize.Width == double.PositiveInfinity ? DefaultWidth : availableSize.Width,
                DefaultHeight);

        protected override void ArrangeCore(Rect finalRect) => Bounds = finalRect;

        // ── 渲染 ──

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;
            double w = Bounds.Width;
            double centerY = Bounds.Height / 2;

            // 轨道
            var trackRect = new Rect(0, centerY - TrackHeight / 2, w, TrackHeight);
            var track = TrackBrush.ToPlatform(render);
            try { context.FillRoundedRectangle(trackRect, 2, 2, track); }
            finally { track.Dispose(); }

            // 已填充部分(从 Minimum 到 Value)
            double ratio = Maximum > Minimum ? (Value - Minimum) / (Maximum - Minimum) : 0;
            double fillW = Math.Max(0, w * ratio);
            if (fillW > 0)
            {
                var fillRect = new Rect(0, centerY - TrackHeight / 2, fillW, TrackHeight);
                var fill = ThumbBrush.ToPlatform(render);
                try { context.FillRoundedRectangle(fillRect, 2, 2, fill); }
                finally { fill.Dispose(); }
            }

            // 滑块
            double thumbX = fillW - ThumbWidth / 2;
            var thumbRect = new Rect(thumbX, centerY - 8, ThumbWidth, 16);
            var thumb = ThumbBrush.ToPlatform(render);
            try { context.FillRoundedRectangle(thumbRect, 3, 3, thumb); }
            finally { thumb.Dispose(); }
        }

        // ── 输入:点击/拖拽 ──

        public override void OnPointerPressed(PointerEventArgs e)
        {
            Focus();
            _isDragging = true;
            CapturePointer();
            UpdateValueFromX(e.Position.X);
            e.Handled = true;
        }

        public override void OnPointerMoved(PointerEventArgs e)
        {
            if (_isDragging)
            {
                UpdateValueFromX(e.Position.X);
                e.Handled = true;
            }
        }

        public override void OnPointerReleased(PointerEventArgs e)
        {
            _isDragging = false;
            ReleasePointerCapture();
            e.Handled = true;
        }

        private void UpdateValueFromX(double x)
        {
            double w = Bounds.Width;
            if (w <= 0) return;
            double ratio = Math.Clamp(x / w, 0, 1);
            Value = Minimum + ratio * (Maximum - Minimum);
        }

        protected internal override void OnKeyDown(Bpf.Input.KeyEventArgs e)
        {
            double step = (Maximum - Minimum) / 20;
            switch (e.Key)
            {
                case Bpf.Input.Key.Left:
                case Bpf.Input.Key.Down:
                    Value = Value - step;
                    e.Handled = true;
                    break;
                case Bpf.Input.Key.Right:
                case Bpf.Input.Key.Up:
                    Value = Value + step;
                    e.Handled = true;
                    break;
            }
        }
    }
}
