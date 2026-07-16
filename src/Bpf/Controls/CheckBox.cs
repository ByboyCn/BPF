using System;
using Bpf.Controls.Routing;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 复选框。点击切换 IsChecked,触发 Checked/Unchecked 事件。
    /// </summary>
    public sealed class CheckBox : Control
    {
        // ── 属性 ──

        public static readonly StyledProperty<bool> IsCheckedProperty =
            StyledProperty<bool>.Register<CheckBox>(nameof(IsChecked), false, affectsRender: true);

        public bool IsChecked
        {
            get => GetValue(IsCheckedProperty);
            set
            {
                SetValue(IsCheckedProperty, value);
                RaiseCheckChanged(value);
            }
        }

        public static readonly StyledProperty<string> ContentProperty =
            StyledProperty<string>.Register<CheckBox>(nameof(Content), "",
            affectsMeasure: true, affectsRender: true);

        public string Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<CheckBox>(nameof(Foreground),
                new SolidColorBrush(Color.Black), affectsRender: true);

        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<Brush> CheckMarkBrushProperty =
            StyledProperty<Brush>.Register<CheckBox>(nameof(CheckMarkBrush),
                new SolidColorBrush(Color.FromRgb(0x00, 0x70, 0xC0)), affectsRender: true);

        public Brush CheckMarkBrush
        {
            get => GetValue(CheckMarkBrushProperty);
            set => SetValue(CheckMarkBrushProperty, value);
        }

        public static readonly StyledProperty<string> FontFamilyProperty =
            StyledProperty<string>.Register<CheckBox>(nameof(FontFamily), "Segoe UI",
                affectsMeasure: true, affectsRender: true);

        public string FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly StyledProperty<double> FontSizeProperty =
            StyledProperty<double>.Register<CheckBox>(nameof(FontSize), 14.0,
                affectsMeasure: true, affectsRender: true);

        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        // ── 路由事件 ──

        public static readonly RoutedEvent<RoutedEventArgs> CheckedEvent =
            RoutedEvent<RoutedEventArgs>.Register<CheckBox>(nameof(Checked), RoutingStrategies.Bubble);
        public static readonly RoutedEvent<RoutedEventArgs> UncheckedEvent =
            RoutedEvent<RoutedEventArgs>.Register<CheckBox>(nameof(Unchecked), RoutingStrategies.Bubble);

        public event EventHandler<RoutedEventArgs>? Checked
        {
            add => AddHandler(CheckedEvent, value!);
            remove => RemoveHandler(CheckedEvent, value!);
        }
        public event EventHandler<RoutedEventArgs>? Unchecked
        {
            add => AddHandler(UncheckedEvent, value!);
            remove => RemoveHandler(UncheckedEvent, value!);
        }

        private void RaiseCheckChanged(bool newValue)
        {
            RaiseEvent(newValue ? CheckedEvent : UncheckedEvent, new RoutedEventArgs());
        }

        // ── 缓存文本格式 ──

        private IPlatformTextFormat? _format;

        public CheckBox()
        {
            IsFocusable = true;
        }

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

        private IPlatformTextFormat EnsureFormat()
        {
            if (_format is null) RebuildFormat();
            return _format!;
        }

        // ── 布局 ──

        private const double BoxSize = 16;
        private const double Gap = 6;

        protected override Size MeasureCore(Size availableSize)
        {
            double w = BoxSize, h = BoxSize;
            if (!string.IsNullOrEmpty(Content))
            {
                var fmt = EnsureFormat();
                var m = fmt.MeasureText(Content);
                w += Gap + m.Width;
                h = Math.Max(h, m.Height);
            }
            return new Size(w, h);
        }

        protected override void ArrangeCore(Rect finalRect) => Bounds = finalRect;

        // ── 渲染 ──

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;

            // 悬停时方框背景(主题悬停色,支持亮/暗切换)
            if (IsPointerOver)
            {
                var hoverBg = new SolidColorBrush(Bpf.Theming.Theme.Current.HoverBackground).ToPlatform(render);
                try { context.FillRectangle(new Rect(0, 0, BoxSize, BoxSize), hoverBg); }
                finally { hoverBg.Dispose(); }
            }

            // 方框
            var boxRect = new Rect(0, 0, BoxSize, BoxSize);
            var border = CheckMarkBrush.ToPlatform(render);
            try
            {
                context.DrawRectangle(boxRect, border, 1.5);
            }
            finally { border.Dispose(); }

            // 勾选标记:画一个小填充方块
            if (IsChecked)
            {
                var fill = CheckMarkBrush.ToPlatform(render);
                try
                {
                    var inset = 3;
                    context.FillRectangle(
                        new Rect(inset, inset, BoxSize - inset * 2, BoxSize - inset * 2), fill);
                }
                finally { fill.Dispose(); }
            }

            // 文字
            if (!string.IsNullOrEmpty(Content))
            {
                var fmt = EnsureFormat();
                var fg = Foreground.ToPlatform(render);
                try
                {
                    context.DrawText(new Point(BoxSize + Gap, 0), Content, fmt, fg);
                }
                finally { fg.Dispose(); }
            }
        }

        protected internal override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            InvalidateVisual();
        }

        protected internal override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            InvalidateVisual();
        }

        // ── 输入 ──

        public override void OnPointerPressed(PointerEventArgs e)
        {
            IsChecked = !IsChecked;
            e.Handled = true;
        }

        protected internal override void OnKeyDown(Bpf.Input.KeyEventArgs e)
        {
            if (IsFocused && e.Key == Bpf.Input.Key.Space)
            {
                IsChecked = !IsChecked;
                e.Handled = true;
            }
        }
    }
}
