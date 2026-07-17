using System;
using System.Collections.Generic;
using Bpf.Controls.Routing;
using Bpf.Input;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 单选按钮。同 GroupName 的 RadioButton 互斥(选中一个,其余自动取消)。
    /// </summary>
    public sealed class RadioButton : Control
    {
        // ── 属性 ──

        public static readonly StyledProperty<bool> IsCheckedProperty =
            StyledProperty<bool>.Register<RadioButton>(nameof(IsChecked), false, affectsRender: true);

        public bool IsChecked
        {
            get => GetValue(IsCheckedProperty);
            set
            {
                SetValue(IsCheckedProperty, value);
                if (value) UncheckOthersInGroup();
                RaiseCheckChanged(value);
            }
        }

        public static readonly StyledProperty<string> GroupNameProperty =
            StyledProperty<string>.Register<RadioButton>(nameof(GroupName), "",
            affectsArrange: true);

        public string GroupName
        {
            get => GetValue(GroupNameProperty);
            set => SetValue(GroupNameProperty, value);
        }

        public static readonly StyledProperty<string> ContentProperty =
            StyledProperty<string>.Register<RadioButton>(nameof(Content), "",
            affectsMeasure: true, affectsRender: true);

        public string Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<RadioButton>(nameof(Foreground),
                new SolidColorBrush(Color.Black), affectsRender: true);

        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<Brush> AccentBrushProperty =
            StyledProperty<Brush>.Register<RadioButton>(nameof(AccentBrush),
                new SolidColorBrush(Color.FromRgb(0x00, 0x70, 0xC0)), affectsRender: true);

        public Brush AccentBrush
        {
            get => GetValue(AccentBrushProperty);
            set => SetValue(AccentBrushProperty, value);
        }

        public static readonly StyledProperty<string> FontFamilyProperty =
            StyledProperty<string>.Register<RadioButton>(nameof(FontFamily), "Segoe UI",
                affectsMeasure: true, affectsRender: true);

        public string FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly StyledProperty<double> FontSizeProperty =
            StyledProperty<double>.Register<RadioButton>(nameof(FontSize), 14.0,
                affectsMeasure: true, affectsRender: true);

        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        // ── 路由事件 ──

        public static readonly RoutedEvent<RoutedEventArgs> CheckedEvent =
            RoutedEvent<RoutedEventArgs>.Register<RadioButton>(nameof(Checked), RoutingStrategies.Bubble);
        public static readonly RoutedEvent<RoutedEventArgs> UncheckedEvent =
            RoutedEvent<RoutedEventArgs>.Register<RadioButton>(nameof(Unchecked), RoutingStrategies.Bubble);

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

        // ── 互斥:取消同组其它 RadioButton ──

        private void UncheckOthersInGroup()
        {
            // GroupName 非空:同 GroupName 的互斥。
            // GroupName 为空:同父级的 RadioButton 互斥(默认行为,类似 WPF)。
            if (!string.IsNullOrEmpty(GroupName))
            {
                var root = LogicalRoot ?? this;
                foreach (var rb in FindSameGroup(root, GroupName))
                {
                    if (!ReferenceEquals(rb, this) && rb.IsChecked)
                    {
                        rb.SetValue(IsCheckedProperty, false);
                        rb.RaiseEvent(UncheckedEvent, new RoutedEventArgs());
                    }
                }
            }
            else
            {
                // 同父级互斥
                if (Parent is IPanel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is RadioButton rb && !ReferenceEquals(rb, this) &&
                            string.IsNullOrEmpty(rb.GroupName) && rb.IsChecked)
                        {
                            rb.SetValue(IsCheckedProperty, false);
                            rb.RaiseEvent(UncheckedEvent, new RoutedEventArgs());
                        }
                    }
                }
            }
        }

        private static List<RadioButton> FindSameGroup(Control root, string groupName)
        {
            var result = new List<RadioButton>();
            Collect(root, result, groupName);
            return result;
        }

        private static void Collect(Control c, List<RadioButton> result, string groupName)
        {
            if (c is RadioButton target && target.GroupName == groupName)
            {
                result.Add(target);
            }
            if (c is IPanel panel)
            {
                foreach (var child in panel.Children)
                    Collect(child, result, groupName);
            }
        }

        // ── 缓存文本格式 ──

        private IPlatformTextFormat? _format;

        public RadioButton()
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

        private const double CircleSize = 16;
        private const double Gap = 6;

        protected override Size MeasureCore(Size availableSize)
        {
            double w = CircleSize, h = CircleSize;
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

            // 圆圈(用圆角矩形近似圆,radius = CircleSize/2)
            var circleRect = new Rect(0, 0, CircleSize, CircleSize);
            var border = AccentBrush.ToPlatform(render);
            try
            {
                context.DrawRoundedRectangle(circleRect, CircleSize / 2, CircleSize / 2, border, 1.5);
            }
            finally { border.Dispose(); }

            // 选中标记:中心小圆点(用圆角矩形近似)
            if (IsChecked)
            {
                var fill = AccentBrush.ToPlatform(render);
                try
                {
                    var dotInset = 4;
                    var dotSize = CircleSize - dotInset * 2;
                    context.FillRoundedRectangle(
                        new Rect(dotInset, dotInset, dotSize, dotSize),
                        dotSize / 2, dotSize / 2, fill);
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
                    context.DrawText(new Point(CircleSize + Gap, 0), Content, fmt, fg);
                }
                finally { fg.Dispose(); }
            }
        }

        // ── 输入 ──

        public override void OnPointerPressed(PointerEventArgs e)
        {
            if (!IsChecked) IsChecked = true;
            e.Handled = true;
        }

        protected internal override void OnKeyDown(KeyEventArgs e)
        {
            if (IsFocused && e.Key == Key.Space && !IsChecked)
            {
                IsChecked = true;
                e.Handled = true;
            }
        }
    }
}
