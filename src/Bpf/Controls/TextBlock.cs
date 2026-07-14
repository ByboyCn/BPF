using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 显示一段文本的控件。完整 Measure/Arrange/Render。
    /// </summary>
    public sealed class TextBlock : Control
    {
        // ── 属性 ──

        public static readonly StyledProperty<string> TextProperty =
            StyledProperty<string>.Register<TextBlock>(
                nameof(Text), "",
                affectsMeasure: true, affectsRender: true);

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<TextBlock>(
                nameof(Foreground),
                new SolidColorBrush(Color.Black),
                affectsRender: true);

        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<string> FontFamilyProperty =
            StyledProperty<string>.Register<TextBlock>(
                nameof(FontFamily), "Segoe UI",
                affectsMeasure: true, affectsRender: true);

        public string FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly StyledProperty<double> FontSizeProperty =
            StyledProperty<double>.Register<TextBlock>(
                nameof(FontSize), 14.0,
                affectsMeasure: true, affectsRender: true);

        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly StyledProperty<FontWeight> FontWeightProperty =
            StyledProperty<FontWeight>.Register<TextBlock>(
                nameof(FontWeight), Bpf.Platform.FontWeight.Normal,
                affectsMeasure: true, affectsRender: true);

        public FontWeight FontWeight
        {
            get => GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        // 缓存的平台文本格式(在 HostWindow 的 RenderInterface 上创建)
        private IPlatformTextFormat? _format;
        private string? _lastFontFamily;
        private double _lastFontSize;
        private FontWeight _lastFontWeight;

        protected override void OnAttachedToHost()
        {
            base.OnAttachedToHost();
            RebuildFormat();
        }

        protected override void OnPropertyChanged<TValue>(
            StyledProperty<TValue> property, object? oldValue, TValue newValue)
        {
            base.OnPropertyChanged(property, oldValue, newValue);

            // 字体相关变更 → 重建文本格式
            if (ReferenceEquals(property, FontFamilyProperty) ||
                ReferenceEquals(property, FontSizeProperty) ||
                ReferenceEquals(property, FontWeightProperty))
            {
                RebuildFormat();
            }
        }

        private void RebuildFormat()
        {
            // 文本格式必须挂在 RenderInterface 上;只有在挂载后才有
            if (HostWindow is null) return;
            // 注意:HostWindow 没有直接暴露 RenderInterface。
            // M1 用一个间接:Application.Current.RenderInterface
            var render = Bpf.Application.Application.Current.RenderInterface;
            _format?.Dispose();
            _format = render.CreateTextFormat(FontFamily, FontSize, FontWeight);
            _lastFontFamily = FontFamily;
            _lastFontSize = FontSize;
            _lastFontWeight = FontWeight;
        }

        protected override Size MeasureCore(Size availableSize)
        {
            if (string.IsNullOrEmpty(Text)) return Size.Empty;

            EnsureFormat();
            if (_format is null) return new Size(0, 0);

            return _format.MeasureText(Text);
        }

        private void EnsureFormat()
        {
            if (_format is null ||
                _lastFontFamily != FontFamily ||
                _lastFontSize != FontSize ||
                _lastFontWeight != FontWeight)
            {
                RebuildFormat();
            }
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = finalRect;
        }

        public override void Render(IDrawingContext context)
        {
            if (string.IsNullOrEmpty(Text) || _format is null) return;

            var fg = Foreground.ToPlatform(
                Bpf.Application.Application.Current.RenderInterface);
            try
            {
                context.DrawText(Point.Origin, Text, _format, fg);
            }
            finally
            {
                fg.Dispose();
            }
        }
    }
}
