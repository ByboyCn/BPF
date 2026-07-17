using System;
using Bpf.Controls.Routing;
using Bpf.Input;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 密码输入框:输入的字符显示为圆点(•),不泄露实际内容。
    /// 内部存储真实密码(Password 属性),渲染时画 PasswordChar(默认 •)代替。
    /// 支持光标、退格、方向键(复用逻辑,但不用 TextBox 子控件以避免显示明文)。
    /// </summary>
    public sealed class PasswordBox : Control
    {
        public static readonly StyledProperty<string> PasswordProperty =
            StyledProperty<string>.Register<PasswordBox>(nameof(Password), "",
                affectsMeasure: true, affectsRender: true);
        public string Password
        {
            get => GetValue(PasswordProperty);
            set
            {
                SetValue(PasswordProperty, value ?? "");
                if (_caretIndex > Password.Length) _caretIndex = Password.Length;
            }
        }

        /// <summary>掩盖字符(默认 •)。显示时每个密码字符用此字符代替。</summary>
        public char PasswordChar { get; set; } = '•';

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<PasswordBox>(nameof(Foreground),
                new SolidColorBrush(Color.Black), affectsRender: true);
        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<Brush> BackgroundProperty =
            StyledProperty<Brush>.Register<PasswordBox>(nameof(Background),
                new SolidColorBrush(Color.White), affectsRender: true);
        public Brush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly StyledProperty<Brush> BorderBrushProperty =
            StyledProperty<Brush>.Register<PasswordBox>(nameof(BorderBrush),
                new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), affectsRender: true);
        public Brush BorderBrush
        {
            get => GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public static readonly StyledProperty<double> FontSizeProperty =
            StyledProperty<double>.Register<PasswordBox>(nameof(FontSize), 14.0,
                affectsMeasure: true, affectsRender: true);
        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        private int _caretIndex;
        private IPlatformTextFormat? _format;

        public PasswordBox()
        {
            IsFocusable = true;
        }

        protected override void OnAttachedToHost()
        {
            base.OnAttachedToHost();
            RebuildFormat();
        }

        private void RebuildFormat()
        {
            if (HostWindow is null) return;
            _format?.Dispose();
            _format = Bpf.Application.Application.Current.RenderInterface.CreateTextFormat(
                "Segoe UI", FontSize, FontWeight.Normal);
        }

        private IPlatformTextFormat EnsureFormat()
        {
            if (_format is null) RebuildFormat();
            return _format!;
        }

        /// <summary>掩盖后的显示文本(每个字符替换为 PasswordChar)。</summary>
        private string MaskedText => new string(PasswordChar, Password.Length);

        protected override Size MeasureCore(Size availableSize)
        {
            var fmt = EnsureFormat();
            var m = fmt.MeasureText(new string(PasswordChar, 8)); // 用 8 个圆点估宽度
            return new Size(Math.Max(120, m.Width + 8), m.Height + 8);
        }

        protected override void ArrangeCore(Rect finalRect) => Bounds = finalRect;

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;
            var bg = Background.ToPlatform(render);
            var border = BorderBrush.ToPlatform(render);
            try
            {
                context.FillRectangle(new Rect(0, 0, Bounds.Width, Bounds.Height), bg);
                context.DrawRectangle(new Rect(0.5, 0.5, Bounds.Width - 1, Bounds.Height - 1), border, 1.0);
            }
            finally { bg.Dispose(); border.Dispose(); }

            var pad = 4.0;
            var innerRect = new Rect(pad, pad, Bounds.Width - pad * 2, Bounds.Height - pad * 2);
            context.PushClip(innerRect);
            try
            {
                var fmt = EnsureFormat();
                var masked = MaskedText;
                if (masked.Length > 0)
                {
                    var fg = Foreground.ToPlatform(render);
                    try { context.DrawText(new Point(pad, pad), masked, fmt, fg); }
                    finally { fg.Dispose(); }
                }

                // 光标(聚焦时)
                if (IsFocused)
                {
                    var before = masked.Substring(0, Math.Min(_caretIndex, masked.Length));
                    double caretX = pad + (before.Length > 0 ? fmt.MeasureText(before).Width : 0);
                    var caretBrush = Foreground.ToPlatform(render);
                    try { context.FillRectangle(new Rect(caretX, pad, 1, innerRect.Height), caretBrush); }
                    finally { caretBrush.Dispose(); }
                }
            }
            finally { context.PopClip(); }
        }

        public override void OnPointerPressed(PointerEventArgs e)
        {
            Focus();
            var fmt = EnsureFormat();
            double x = e.Position.X - 4;
            if (x <= 0) { _caretIndex = 0; InvalidateVisual(); e.Handled = true; return; }
            double acc = 0;
            for (int i = 0; i < Password.Length; i++)
            {
                var w = fmt.MeasureText(PasswordChar.ToString()).Width;
                if (x < acc + w / 2) { _caretIndex = i; InvalidateVisual(); e.Handled = true; return; }
                acc += w;
            }
            _caretIndex = Password.Length;
            InvalidateVisual();
            e.Handled = true;
        }

        protected internal override void OnTextInput(TextEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text)) return;
            foreach (var ch in e.Text)
            {
                if (ch < 32) continue;
                Password = Password.Insert(_caretIndex, ch.ToString());
                _caretIndex++;
            }
            InvalidateVisual();
            e.Handled = true;
        }

        protected internal override void OnKeyDown(Bpf.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Bpf.Input.Key.Backspace:
                    if (_caretIndex > 0) { Password = Password.Remove(_caretIndex - 1, 1); _caretIndex--; }
                    e.Handled = true; break;
                case Bpf.Input.Key.Delete:
                    if (_caretIndex < Password.Length) Password = Password.Remove(_caretIndex, 1);
                    e.Handled = true; break;
                case Bpf.Input.Key.Left:
                    if (_caretIndex > 0) _caretIndex--; e.Handled = true; break;
                case Bpf.Input.Key.Right:
                    if (_caretIndex < Password.Length) _caretIndex++; e.Handled = true; break;
                case Bpf.Input.Key.Home:
                    _caretIndex = 0; e.Handled = true; break;
                case Bpf.Input.Key.End:
                    _caretIndex = Password.Length; e.Handled = true; break;
            }
            InvalidateVisual();
        }
    }
}
