using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 文本标签。显示一段文字,可关联另一个控件的 Target(点击 Label 聚焦 Target)。
    /// M4.1 等同 TextBlock + Target 属性。
    /// </summary>
    public sealed class Label : Control
    {
        public static readonly StyledProperty<string> TextProperty =
            StyledProperty<string>.Register<Label>(nameof(Text), "",
                affectsMeasure: true, affectsRender: true);

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<Label>(nameof(Foreground),
                new SolidColorBrush(Color.Black), affectsRender: true);

        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<string> FontFamilyProperty =
            StyledProperty<string>.Register<Label>(nameof(FontFamily), "Segoe UI",
                affectsMeasure: true, affectsRender: true);

        public string FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly StyledProperty<double> FontSizeProperty =
            StyledProperty<double>.Register<Label>(nameof(FontSize), 14.0,
                affectsMeasure: true, affectsRender: true);

        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>关联的目标控件(点击 Label 时聚焦它)。null = 无关联。</summary>
        public Control? Target { get; set; }

        private IPlatformTextFormat? _format;

        protected override void OnAttachedToHost()
        {
            base.OnAttachedToHost();
            RebuildFormat();
        }

        protected override void OnPropertyChanged<TValue>(
            StyledProperty<TValue> property, object? oldValue, TValue newValue)
        {
            base.OnPropertyChanged(property, oldValue, newValue);
            if (ReferenceEquals(property, FontFamilyProperty) ||
                ReferenceEquals(property, FontSizeProperty))
            {
                RebuildFormat();
            }
        }

        private void RebuildFormat()
        {
            if (HostWindow is null) return;
            _format?.Dispose();
            _format = Bpf.Application.Application.Current.RenderInterface.CreateTextFormat(
                FontFamily, FontSize, FontWeight.Normal);
        }

        protected override Size MeasureCore(Size availableSize)
        {
            if (string.IsNullOrEmpty(Text)) return Size.Empty;
            if (_format is null) RebuildFormat();
            return _format?.MeasureText(Text) ?? Size.Empty;
        }

        protected override void ArrangeCore(Rect finalRect) => Bounds = finalRect;

        public override void Render(IDrawingContext context)
        {
            if (string.IsNullOrEmpty(Text) || _format is null) return;
            var fg = Foreground.ToPlatform(Bpf.Application.Application.Current.RenderInterface);
            try { context.DrawText(Point.Origin, Text, _format, fg); }
            finally { fg.Dispose(); }
        }

        public override void OnPointerPressed(PointerEventArgs e)
        {
            // 点击 Label 聚焦关联的 Target
            Target?.Focus();
        }

        public override bool HitTest(Point point) =>
            IsVisible && Bounds.Contains(point);
    }
}
