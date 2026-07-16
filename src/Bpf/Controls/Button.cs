using System;
using Bpf.Controls.Routing;
using Bpf.Input;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 标准按钮控件。显示文字,响应点击。
    /// </summary>
    public sealed class Button : Control
    {
        private readonly TextBlock _textBlock;
        private bool _isPressed;

        // ── 属性 ──

        public static readonly StyledProperty<string> ContentProperty =
            StyledProperty<string>.Register<Button>(
                nameof(Content), "",
                affectsMeasure: true, affectsRender: true);

        public string Content
        {
            get => GetValue(ContentProperty);
            set
            {
                SetValue(ContentProperty, value!);
                _textBlock.Text = value;
            }
        }

        public static readonly StyledProperty<Brush> BackgroundProperty =
            StyledProperty<Brush>.Register<Button>(
                nameof(Background),
                new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)),
                affectsRender: true);

        public Brush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value!);
        }

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<Button>(
                nameof(Foreground),
                new SolidColorBrush(Color.Black),
                affectsRender: true);

        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set
            {
                SetValue(ForegroundProperty, value!);
                _textBlock.Foreground = value;
            }
        }

        public static readonly StyledProperty<Brush> BorderBrushProperty =
            StyledProperty<Brush>.Register<Button>(
                nameof(BorderBrush),
                new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                affectsRender: true);

        public Brush BorderBrush
        {
            get => GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value!);
        }

        public static readonly StyledProperty<double> PaddingProperty =
            StyledProperty<double>.Register<Button>(
                nameof(Padding), 8.0,
                affectsMeasure: true, affectsArrange: true);

        public double Padding
        {
            get => GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value!);
        }

        public static readonly StyledProperty<double> FontSizeProperty =
            StyledProperty<double>.Register<Button>(
                nameof(FontSize), 14.0,
                affectsMeasure: true, affectsRender: true);

        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set
            {
                SetValue(FontSizeProperty, value!);
                _textBlock.FontSize = value;
            }
        }

        public static readonly StyledProperty<string> FontFamilyProperty =
            StyledProperty<string>.Register<Button>(
                nameof(FontFamily), "Segoe UI",
                affectsMeasure: true, affectsRender: true);

        public string FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set
            {
                SetValue(FontFamilyProperty, value!);
                _textBlock.FontFamily = value;
            }
        }

        // ── 路由事件 ──

        /// <summary>
        /// Click 路由事件(冒泡)。鼠标按下并抬起时、或按钮聚焦时按 Enter/Space 触发。
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
            RoutedEvent<RoutedEventArgs>.Register<Button>(
                nameof(Click), RoutingStrategies.Bubble);

        /// <summary>
        /// 点击事件。订阅等价于 <c>AddHandler(ClickEvent, handler)</c>。
        /// </summary>
        public event EventHandler<RoutedEventArgs>? Click
        {
            add => AddHandler(ClickEvent, value!);
            remove => RemoveHandler(ClickEvent, value!);
        }

        public Button()
        {
            _textBlock = new TextBlock();
            _textBlock.Parent = this;
            // Button 默认可聚焦(可被 Tab 选中,响应 Enter/Space)
            IsFocusable = true;
        }

        protected override void OnAttachedToHost()
        {
            base.OnAttachedToHost();
        }

        protected override void AttachNonPanelChildren(IPlatformWindow window, Control logicalRoot)
        {
            // Button 内部的 TextBlock 不是通过 Children 暴露的,手工 attach
            _textBlock.AttachToHost(window, logicalRoot);
        }

        protected override Size MeasureCore(Size availableSize)
        {
            var pad = Padding;
            _textBlock.Measure(availableSize);
            var text = _textBlock.DesiredSize;
            return new Size(text.Width + pad * 2, text.Height + pad * 2);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = finalRect;
            var pad = Padding;
            var innerRect = new Rect(
                finalRect.X + pad, finalRect.Y + pad,
                Math.Max(0, finalRect.Width - pad * 2),
                Math.Max(0, finalRect.Height - pad * 2));
            _textBlock.Arrange(innerRect);
        }

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;
            var bg = Background.ToPlatform(render);
            var border = BorderBrush.ToPlatform(render);
            try
            {
                // 背景:按下 > 悬停 > 默认。颜色从 Theme 读(支持亮/暗主题切换)。
                IPlatformBrush bgBrush;
                bool disposeBg = false;
                if (_isPressed)
                {
                    bgBrush = new SolidColorBrush(Bpf.Theming.Theme.Current.PressedBackground).ToPlatform(render);
                    disposeBg = true;
                }
                else if (IsPointerOver)
                {
                    bgBrush = new SolidColorBrush(Bpf.Theming.Theme.Current.HoverBackground).ToPlatform(render);
                    disposeBg = true;
                }
                else
                {
                    bgBrush = bg;
                }

                context.FillRoundedRectangle(
                    new Rect(0.5, 0.5, Bounds.Width - 1, Bounds.Height - 1),
                    3, 3, bgBrush);

                if (disposeBg) bgBrush.Dispose();

                // 边框
                context.DrawRoundedRectangle(
                    new Rect(0.5, 0.5, Bounds.Width - 1, Bounds.Height - 1),
                    3, 3, border, 1.0);
            }
            finally
            {
                bg.Dispose();
                border.Dispose();
            }

            // 文字
            var pad = Padding;
            context.PushTranslate(new Vector(pad, pad));
            try
            {
                _textBlock.Render(context);
            }
            finally
            {
                context.PopTransform();
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
            _isPressed = true;
            InvalidateVisual();
            e.Handled = true;
        }

        public override void OnPointerReleased(PointerEventArgs e)
        {
            if (_isPressed)
            {
                _isPressed = false;
                InvalidateVisual();
                // 通过路由事件派发 Click(冒泡到祖先)
                RaiseEvent(ClickEvent, new RoutedEventArgs());
            }
        }

        /// <summary>键盘:聚焦状态下按 Enter/Space 触发 Click。</summary>
        protected internal override void OnKeyDown(KeyEventArgs e)
        {
            if (IsFocused && (e.Key == Bpf.Input.Key.Enter || e.Key == Bpf.Input.Key.Space))
            {
                RaiseEvent(ClickEvent, new RoutedEventArgs());
                e.Handled = true;
            }
        }
    }
}
