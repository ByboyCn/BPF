using System;
using Bpf.Input;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 单行文本输入框。支持文字输入、Backspace/Delete/方向键/Home/End、光标显示。
    /// 聚焦时显示闪烁光标。点击获得焦点。
    /// </summary>
    public sealed class TextBox : Control
    {
        // ── 属性 ──

        public static readonly StyledProperty<string> TextProperty =
            StyledProperty<string>.Register<TextBox>(nameof(Text), "",
                affectsMeasure: true, affectsRender: true);

        public string Text
        {
            get => GetValue(TextProperty);
            set
            {
                var v = value ?? "";
                SetValue(TextProperty, v);
                if (_caretIndex > v.Length) _caretIndex = v.Length;
            }
        }

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<TextBox>(nameof(Foreground),
                new SolidColorBrush(Color.Black), affectsRender: true);

        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<Brush> BackgroundProperty =
            StyledProperty<Brush>.Register<TextBox>(nameof(Background),
                new SolidColorBrush(Color.White), affectsRender: true);

        public Brush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly StyledProperty<Brush> BorderBrushProperty =
            StyledProperty<Brush>.Register<TextBox>(nameof(BorderBrush),
                new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), affectsRender: true);

        public Brush BorderBrush
        {
            get => GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public static readonly StyledProperty<string> FontFamilyProperty =
            StyledProperty<string>.Register<TextBox>(nameof(FontFamily), "Segoe UI",
                affectsMeasure: true, affectsRender: true);

        public string FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly StyledProperty<double> FontSizeProperty =
            StyledProperty<double>.Register<TextBox>(nameof(FontSize), 14.0,
                affectsMeasure: true, affectsRender: true);

        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>光标位置(字符索引)。</summary>
        public int CaretIndex
        {
            get => _caretIndex;
            set => _caretIndex = Math.Clamp(value, 0, Text.Length);
        }

        private int _caretIndex;

        // 缓存的文本格式(参照 TextBlock)
        private IPlatformTextFormat? _format;

        public TextBox()
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

        // ── 布局 ──

        protected override Size MeasureCore(Size availableSize)
        {
            // 默认尺寸:宽度足够显示若干字符,高度按字体
            var fmt = EnsureFormat();
            if (string.IsNullOrEmpty(Text))
            {
                // 空文本时用一个占位测量得到行高
                var m = fmt.MeasureText(" ");
                return new Size(120, m.Height + 8); // 默认宽 120,留 4px 上下边距
            }
            var measured = fmt.MeasureText(Text);
            return new Size(Math.Max(120, measured.Width + 8), measured.Height + 8);
        }

        private IPlatformTextFormat EnsureFormat()
        {
            if (_format is null) RebuildFormat();
            return _format!;
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = finalRect;
        }

        // ── 渲染 ──

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;
            var bg = Background.ToPlatform(render);
            var border = BorderBrush.ToPlatform(render);
            try
            {
                // 背景
                context.FillRectangle(new Rect(0, 0, Bounds.Width, Bounds.Height), bg);
                // 边框
                context.DrawRectangle(new Rect(0.5, 0.5, Bounds.Width - 1, Bounds.Height - 1), border, 1.0);
            }
            finally { bg.Dispose(); border.Dispose(); }

            // 文字(裁剪到内框)
            var pad = 4.0;
            var innerRect = new Rect(pad, pad, Bounds.Width - pad * 2, Bounds.Height - pad * 2);
            context.PushClip(innerRect);
            try
            {
                var fmt = EnsureFormat();
                if (!string.IsNullOrEmpty(Text))
                {
                    var fg = Foreground.ToPlatform(render);
                    try
                    {
                        // 绘制光标位置之前的文字 + 光标后的文字(简单:绘制全部,光标单独画)
                        context.DrawText(new Point(pad, pad), Text, fmt, fg);
                    }
                    finally { fg.Dispose(); }
                }

                // 光标(聚焦时显示竖线)
                if (IsFocused)
                {
                    var caretX = pad + MeasureCaretOffset(fmt);
                    var caretH = Bounds.Height - pad * 2;
                    var caretBrush = Foreground.ToPlatform(render);
                    try
                    {
                        context.FillRectangle(
                            new Rect(caretX, pad, 1, caretH), caretBrush);
                    }
                    finally { caretBrush.Dispose(); }
                }
            }
            finally { context.PopClip(); }
        }

        /// <summary>测量光标前文字的像素宽度。</summary>
        private double MeasureCaretOffset(IPlatformTextFormat fmt)
        {
            if (_caretIndex <= 0 || string.IsNullOrEmpty(Text)) return 0;
            var before = Text.Substring(0, _caretIndex);
            var m = fmt.MeasureText(before);
            return m.Width;
        }

        // ── 输入 ──

        public override void OnPointerPressed(PointerEventArgs e)
        {
            Focus();
            // 点击位置设置光标(简化:根据 X 坐标找最近的字符边界)
            var fmt = EnsureFormat();
            double x = e.Position.X - 4; // 减去内边距
            if (x <= 0) { _caretIndex = 0; e.Handled = true; return; }

            // 逐字符测量找到光标位置
            double acc = 0;
            for (int i = 0; i < Text.Length; i++)
            {
                var w = fmt.MeasureText(Text[i].ToString()).Width;
                if (x < acc + w / 2) { _caretIndex = i; e.Handled = true; return; }
                acc += w;
            }
            _caretIndex = Text.Length;
            InvalidateVisual();
            e.Handled = true;
        }

        protected internal override void OnTextInput(TextEventArgs e)
        {
            // 追加文本到光标位置
            if (string.IsNullOrEmpty(e.Text)) return;
            foreach (var ch in e.Text)
            {
                // 忽略控制字符
                if (ch < 32) continue;
                Text = Text.Insert(_caretIndex, ch.ToString());
                _caretIndex++;
            }
            InvalidateVisual();
            e.Handled = true;
        }

        protected internal override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Backspace:
                    if (_caretIndex > 0)
                    {
                        Text = Text.Remove(_caretIndex - 1, 1);
                        _caretIndex--;
                        InvalidateVisual();
                    }
                    e.Handled = true;
                    break;

                case Key.Delete:
                    if (_caretIndex < Text.Length)
                    {
                        Text = Text.Remove(_caretIndex, 1);
                        InvalidateVisual();
                    }
                    e.Handled = true;
                    break;

                case Key.Left:
                    if (_caretIndex > 0) { _caretIndex--; InvalidateVisual(); }
                    e.Handled = true;
                    break;

                case Key.Right:
                    if (_caretIndex < Text.Length) { _caretIndex++; InvalidateVisual(); }
                    e.Handled = true;
                    break;

                case Key.Home:
                    _caretIndex = 0; InvalidateVisual();
                    e.Handled = true;
                    break;

                case Key.End:
                    _caretIndex = Text.Length; InvalidateVisual();
                    e.Handled = true;
                    break;
            }
        }
    }
}
