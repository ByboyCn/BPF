using System;
using Bpf.Controls.Routing;
using Bpf.Input;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 数字微调框:显示数值,可用上下箭头按钮递增/递减,也可直接键盘输入。
    /// 内部用一个 TextBox 显示值,右侧两个小按钮(▲▼)做增减。
    /// 支持 Minimum/Maximum/Increment/Value,值变化触发 ValueChanged 事件。
    /// </summary>
    public sealed class NumericUpDown : Control, IContentHost
    {
        private readonly TextBox _textBox;
        private IPlatformTextFormat? _format;

        public static readonly StyledProperty<double> ValueProperty =
            StyledProperty<double>.Register<NumericUpDown>(nameof(Value), 0.0,
                affectsMeasure: true, affectsRender: true);
        public double Value
        {
            get => GetValue(ValueProperty);
            set
            {
                var clamped = Math.Clamp(value, Minimum, Maximum);
                SetValue(ValueProperty, clamped);
                if (_textBox != null) _textBox.Text = clamped.ToString();
                RaiseEvent(ValueChangedEvent, new RoutedEventArgs());
                InvalidateVisual();
            }
        }

        public static readonly StyledProperty<double> MinimumProperty =
            StyledProperty<double>.Register<NumericUpDown>(nameof(Minimum), 0.0, affectsRender: true);
        public double Minimum
        {
            get => GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly StyledProperty<double> MaximumProperty =
            StyledProperty<double>.Register<NumericUpDown>(nameof(Maximum), 100.0, affectsRender: true);
        public double Maximum
        {
            get => GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        /// <summary>每次增减的步长。默认 1。</summary>
        public static readonly StyledProperty<double> IncrementProperty =
            StyledProperty<double>.Register<NumericUpDown>(nameof(Increment), 1.0);
        public double Increment
        {
            get => GetValue(IncrementProperty);
            set => SetValue(IncrementProperty, value);
        }

        public static readonly RoutedEvent<RoutedEventArgs> ValueChangedEvent =
            RoutedEvent<RoutedEventArgs>.Register<NumericUpDown>(nameof(ValueChanged), RoutingStrategies.Bubble);
        public event EventHandler<RoutedEventArgs>? ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value!);
            remove => RemoveHandler(ValueChangedEvent, value!);
        }

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<NumericUpDown>(nameof(Foreground),
                new SolidColorBrush(Color.Black), affectsRender: true);
        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<Brush> BackgroundProperty =
            StyledProperty<Brush>.Register<NumericUpDown>(nameof(Background),
                new SolidColorBrush(Color.White), affectsRender: true);
        public Brush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly StyledProperty<Brush> BorderBrushProperty =
            StyledProperty<Brush>.Register<NumericUpDown>(nameof(BorderBrush),
                new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), affectsRender: true);
        public Brush BorderBrush
        {
            get => GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        Control? IContentHost.ContentChild => _textBox;

        private const double ButtonW = 18; // 增减按钮宽度
        private bool _upHover, _downHover;

        public NumericUpDown()
        {
            _textBox = new TextBox { Text = "0", IsFocusable = true };
            _textBox.Parent = this;
            _textBox.TextChanged += (s, e) =>
            {
                // 用户输入:尝试解析回 Value
                if (double.TryParse(_textBox.Text, out var v))
                    SetValue(ValueProperty, Math.Clamp(v, Minimum, Maximum));
            };
            IsFocusable = false;
        }

        protected override void OnAttachedToHost()
        {
            base.OnAttachedToHost();
            RebuildFormat();
            _textBox.Text = Value.ToString();
        }

        protected override void AttachNonPanelChildren(IPlatformWindow window, Control logicalRoot)
        {
            _textBox.AttachToHost(window, logicalRoot);
        }

        private void RebuildFormat()
        {
            if (HostWindow is null) return;
            _format?.Dispose();
            _format = Bpf.Application.Application.Current.RenderInterface.CreateTextFormat(
                "Segoe UI", 12, FontWeight.Normal);
        }

        private IPlatformTextFormat EnsureFormat()
        {
            if (_format is null) RebuildFormat();
            return _format!;
        }

        protected override Size MeasureCore(Size availableSize)
        {
            var fmt = EnsureFormat();
            var m = fmt.MeasureText("00");
            return new Size(Math.Max(100, m.Width + ButtonW + 12), 24);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = finalRect;
            // TextBox 占左侧(减去按钮宽度),按钮在右侧
            _textBox.Arrange(new Rect(
                finalRect.X, finalRect.Y,
                Math.Max(0, finalRect.Width - ButtonW), finalRect.Height));
        }

        public override void Render(IDrawingContext context)
        {
            // TextBox 自己渲染。这里只画右侧两个增减按钮。
            var render = Bpf.Application.Application.Current.RenderInterface;
            double bx = Bounds.Width - ButtonW;
            double halfH = Bounds.Height / 2.0;

            // 按钮(三角):上▲ 下▼
            var btnBrush = (_upHover || _downHover
                ? new SolidColorBrush(Bpf.Theming.Theme.Current.HoverBackground)
                : new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE))).ToPlatform(render);
            var arrow = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40)).ToPlatform(render);
            try
            {
                context.FillRectangle(new Rect(bx, 0, ButtonW, Bounds.Height), btnBrush);
                // 分隔线
                var sep = BorderBrush.ToPlatform(render);
                try { context.FillRectangle(new Rect(bx, 0, 1, Bounds.Height), sep); }
                finally { sep.Dispose(); }
                // 中间分隔线
                context.FillRectangle(new Rect(bx + 1, halfH, ButtonW - 1, 1), BorderBrush.ToPlatform(render));

                // 上三角(▲)
                double cx = bx + ButtonW / 2.0;
                context.FillTriangle(new Point(cx - 4, halfH - 3), new Point(cx + 4, halfH - 3), new Point(cx, halfH - 8), arrow);
                // 下三角(▼)
                context.FillTriangle(new Point(cx - 4, halfH + 3), new Point(cx + 4, halfH + 3), new Point(cx, halfH + 8), arrow);
            }
            finally { btnBrush.Dispose(); arrow.Dispose(); }

            // 渲染 TextBox
            var relX = _textBox.Bounds.X - Bounds.X;
            var relY = _textBox.Bounds.Y - Bounds.Y;
            context.PushTranslate(new Vector(relX, relY));
            try { _textBox.Render(context); }
            finally { context.PopTransform(); }
        }

        // ── 输入:点击按钮增减;TextBox 接收键盘输入 ──

        public override void OnPointerPressed(PointerEventArgs e)
        {
            double bx = Bounds.Width - ButtonW;
            if (e.Position.X >= bx)
            {
                Focus();
                double halfH = Bounds.Height / 2.0;
                if (e.Position.Y < halfH)
                    Value += Increment;
                else
                    Value -= Increment;
                e.Handled = true;
                return;
            }
            // 点击文本区:让 TextBox 获焦
            _textBox.Focus();
        }

        protected internal override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            UpdateButtonHover(e.Position.X, e.Position.Y);
        }

        public override void OnPointerMoved(PointerEventArgs e)
        {
            UpdateButtonHover(e.Position.X, e.Position.Y);
        }

        protected internal override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            if (_upHover || _downHover) { _upHover = _downHover = false; InvalidateVisual(); }
        }

        private void UpdateButtonHover(double x, double y)
        {
            double bx = Bounds.Width - ButtonW;
            double halfH = Bounds.Height / 2.0;
            bool up = x >= bx && y < halfH;
            bool down = x >= bx && y >= halfH;
            if (up != _upHover || down != _downHover)
            {
                _upHover = up; _downHover = down;
                InvalidateVisual();
            }
        }

        protected internal override void OnKeyDown(KeyEventArgs e)
        {
            // 上/下方向键增减
            if (e.Key == Key.Up) { Value += Increment; e.Handled = true; }
            else if (e.Key == Key.Down) { Value -= Increment; e.Handled = true; }
        }

        public override bool HitTest(Point point)
        {
            if (!IsVisible || !Bounds.Contains(point)) return false;
            return true; // 整个控件接受点击(按钮区自己处理,文本区转发 TextBox)
        }
    }
}
