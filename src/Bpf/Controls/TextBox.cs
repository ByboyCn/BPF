using System;
using Bpf.Controls.Routing;
using Bpf.Input;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;
using Bpf.Threading;

namespace Bpf.Controls
{
    /// <summary>
    /// 单行文本输入框。M7 增强:
    /// - 光标闪烁(500ms 周期,聚焦时启动,失焦/失活时停止)
    /// - 文本选区(Shift+点击/方向键选区、Ctrl+A 全选、Backspace/Delete 删选区、剪贴板预留)
    /// - 光标滚入可见区(文本超出时自动水平滚动,光标始终可见)
    /// </summary>
    public sealed class TextBox : Control, ITickable
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
                ClampSelection();
            }
        }

        // ── 路由事件:文本变化 ──

        public static readonly RoutedEvent<RoutedEventArgs> TextChangedEvent =
            RoutedEvent<RoutedEventArgs>.Register<TextBox>(nameof(TextChanged),
                RoutingStrategies.Bubble);

        public event EventHandler<RoutedEventArgs>? TextChanged
        {
            add => AddHandler(TextChangedEvent, value!);
            remove => RemoveHandler(TextChangedEvent, value!);
        }

        private void RaiseTextChanged()
        {
            RaiseEvent(TextChangedEvent, new RoutedEventArgs());
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

        /// <summary>选区高亮色(默认浅蓝)。</summary>
        public static readonly StyledProperty<Brush> SelectionBrushProperty =
            StyledProperty<Brush>.Register<TextBox>(nameof(SelectionBrush),
                new SolidColorBrush(Color.FromRgb(0x33, 0x99, 0xFF)), affectsRender: true);

        public Brush SelectionBrush
        {
            get => GetValue(SelectionBrushProperty);
            set => SetValue(SelectionBrushProperty, value);
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
            set { _caretIndex = Math.Clamp(value, 0, Text.Length); InvalidateVisual(); }
        }

        private int _caretIndex;
        /// <summary>选区起点(anchor)。等于 _caretIndex 表示无选区。</summary>
        private int _selectionAnchor;
        /// <summary>水平滚动偏移(像素)。文本左端相对于内框的偏移,使光标保持可见。</summary>
        private double _scrollOffset;
        // 光标闪烁
        private double _blinkAccumSeconds;
        private bool _blinkOn = true;

        // 缓存的文本格式(参照 TextBlock)
        private IPlatformTextFormat? _format;

        public TextBox()
        {
            IsFocusable = true;
        }

        // ── 选区辅助 ──

        /// <summary>选区起始(较小索引)。</summary>
        private int SelectionStart => Math.Min(_caretIndex, _selectionAnchor);
        /// <summary>选区结束(较大索引,不含)。</summary>
        private int SelectionEnd => Math.Max(_caretIndex, _selectionAnchor);
        /// <summary>是否有非空选区。</summary>
        private bool HasSelection => _caretIndex != _selectionAnchor;
        /// <summary>把选区锚点夹到合法范围。</summary>
        private void ClampSelection()
        {
            if (_selectionAnchor > Text.Length) _selectionAnchor = Text.Length;
        }
        /// <summary>清除选区(锚点归位到光标)。</summary>
        private void ClearSelection() => _selectionAnchor = _caretIndex;

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

        // ── 焦点:启停光标闪烁 ──

        protected internal override void OnGotFocus(FocusChangedEventArgs e)
        {
            base.OnGotFocus(e);
            StartBlink();
        }

        protected internal override void OnLostFocus(FocusChangedEventArgs e)
        {
            base.OnLostFocus(e);
            StopBlink();
            ClearSelection(); // 失焦清选区
            InvalidateVisual();
        }

        private void StartBlink()
        {
            _blinkAccumSeconds = 0;
            _blinkOn = true;
            TickRegistry.Register(this);
        }

        private void StopBlink()
        {
            _blinkOn = false;
            TickRegistry.Unregister(this);
        }

        /// <summary>ITickable:每帧推进,累计到 0.5s 切换闪烁状态。返回 true 持续。</summary>
        bool ITickable.Tick(double dtSeconds)
        {
            _blinkAccumSeconds += dtSeconds;
            if (_blinkAccumSeconds >= 0.5)
            {
                _blinkAccumSeconds -= 0.5;
                _blinkOn = !_blinkOn;
                InvalidateVisual();
            }
            return true;
        }

        // ── 布局 ──

        protected override Size MeasureCore(Size availableSize)
        {
            var fmt = EnsureFormat();
            if (string.IsNullOrEmpty(Text))
            {
                var m = fmt.MeasureText(" ");
                return new Size(120, m.Height + 8);
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
                // 更新滚动偏移使光标可见
                EnsureCaretVisible(fmt, innerRect.Width);

                // 选区高亮
                if (HasSelection)
                {
                    var selBrush = SelectionBrush.ToPlatform(render);
                    try
                    {
                        double selX0 = pad - _scrollOffset + MeasureRangeWidth(fmt, 0, SelectionStart);
                        double selW = MeasureRangeWidth(fmt, SelectionStart, SelectionEnd);
                        context.FillRectangle(new Rect(selX0, pad, selW, innerRect.Height), selBrush);
                    }
                    finally { selBrush.Dispose(); }
                }

                // 文字
                if (!string.IsNullOrEmpty(Text))
                {
                    var fg = Foreground.ToPlatform(render);
                    try
                    {
                        context.DrawText(new Point(pad - _scrollOffset, pad), Text, fmt, fg);
                    }
                    finally { fg.Dispose(); }
                }

                // 光标(聚焦且闪烁开启时)
                if (IsFocused && _blinkOn)
                {
                    var caretX = pad - _scrollOffset + MeasureRangeWidth(fmt, 0, _caretIndex);
                    var caretBrush = Foreground.ToPlatform(render);
                    try
                    {
                        context.FillRectangle(new Rect(caretX, pad, 1, innerRect.Height), caretBrush);
                    }
                    finally { caretBrush.Dispose(); }
                }
            }
            finally { context.PopClip(); }
        }

        /// <summary>测量 [start, end) 范围内文字的像素宽度。</summary>
        private double MeasureRangeWidth(IPlatformTextFormat fmt, int start, int end)
        {
            if (start >= end || start < 0) return 0;
            start = Math.Max(0, start);
            end = Math.Min(end, Text.Length);
            if (start >= end) return 0;
            return fmt.MeasureText(Text.Substring(start, end - start)).Width;
        }

        /// <summary>调整 _scrollOffset 使光标位于内框可见范围 [0, innerWidth]。</summary>
        private void EnsureCaretVisible(IPlatformTextFormat fmt, double innerWidth)
        {
            double caretRel = MeasureRangeWidth(fmt, 0, _caretIndex); // 光标相对文本左端
            double caretViewX = caretRel - _scrollOffset;             // 光标在内框中的 X

            if (caretViewX < 0)
            {
                // 光标在左边界外,向右滚
                _scrollOffset = caretRel;
            }
            else if (caretViewX > innerWidth)
            {
                // 光标在右边界外,向左滚,留 8px 余量
                _scrollOffset = caretRel - innerWidth + 8;
            }

            // 文本比内框短时不要无谓偏移
            double textWidth = string.IsNullOrEmpty(Text) ? 0 : fmt.MeasureText(Text).Width;
            if (textWidth <= innerWidth) _scrollOffset = 0;
            if (_scrollOffset < 0) _scrollOffset = 0;
        }

        // ── 输入 ──

        public override void OnPointerPressed(PointerEventArgs e)
        {
            Focus();
            var fmt = EnsureFormat();
            int index = HitTestCharacter(fmt, e.Position.X);

            // Shift+点击:扩展选区(移动光标,不动锚点)
            if (Bpf.Input.Keyboard.IsShiftDown)
            {
                _caretIndex = index;
            }
            else
            {
                _caretIndex = index;
                _selectionAnchor = index; // 普通点击清选区
            }
            InvalidateVisual();
            e.Handled = true;
        }

        /// <summary>把 X 坐标转成字符索引(考虑滚动偏移和内边距)。</summary>
        private int HitTestCharacter(IPlatformTextFormat fmt, double x)
        {
            double rel = x - 4 + _scrollOffset; // 减内边距,加滚动
            if (rel <= 0) return 0;

            // 逐字符测量找最近边界
            double acc = 0;
            for (int i = 0; i < Text.Length; i++)
            {
                var w = fmt.MeasureText(Text[i].ToString()).Width;
                if (rel < acc + w / 2) return i;
                acc += w;
            }
            return Text.Length;
        }

        protected internal override void OnTextInput(TextEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text)) return;
            // 有选区时,输入会替换选区
            if (HasSelection) DeleteSelection();

            foreach (var ch in e.Text)
            {
                if (ch < 32) continue; // 忽略控制字符
                Text = Text.Insert(_caretIndex, ch.ToString());
                _caretIndex++;
            }
            ClearSelection();
            InvalidateVisual();
            RaiseTextChanged();
            e.Handled = true;
        }

        protected internal override void OnKeyDown(Bpf.Input.KeyEventArgs e)
        {
            bool shift = (e.Modifiers & Bpf.Input.KeyModifiers.Shift) != 0;
            bool ctrl = (e.Modifiers & Bpf.Input.KeyModifiers.Control) != 0;
            bool changed = false;

            switch (e.Key)
            {
                case Key.Backspace:
                    if (HasSelection) { DeleteSelection(); changed = true; }
                    else if (_caretIndex > 0)
                    {
                        Text = Text.Remove(_caretIndex - 1, 1);
                        _caretIndex--;
                        changed = true;
                    }
                    ClearSelection();
                    break;

                case Key.Delete:
                    if (HasSelection) { DeleteSelection(); changed = true; }
                    else if (_caretIndex < Text.Length)
                    {
                        Text = Text.Remove(_caretIndex, 1);
                        changed = true;
                    }
                    ClearSelection();
                    break;

                case Key.Left:
                    if (HasSelection && !shift) { _caretIndex = SelectionStart; }
                    else if (_caretIndex > 0) { _caretIndex--; }
                    if (!shift) ClearSelection();
                    break;

                case Key.Right:
                    if (HasSelection && !shift) { _caretIndex = SelectionEnd; }
                    else if (_caretIndex < Text.Length) { _caretIndex++; }
                    if (!shift) ClearSelection();
                    break;

                case Key.Home:
                    _caretIndex = 0;
                    if (!shift) ClearSelection();
                    break;

                case Key.End:
                    _caretIndex = Text.Length;
                    if (!shift) ClearSelection();
                    break;

                case Key.A when ctrl:
                    _selectionAnchor = 0;
                    _caretIndex = Text.Length;
                    break;

                default:
                    return; // 未处理,不设 Handled
            }

            InvalidateVisual();
            if (changed) RaiseTextChanged();
            e.Handled = true;
        }

        /// <summary>删除当前选区内的文字,光标移到选区起点。</summary>
        private void DeleteSelection()
        {
            if (!HasSelection) return;
            int start = SelectionStart, end = SelectionEnd;
            Text = Text.Remove(start, end - start);
            _caretIndex = start;
        }
    }
}
