using System;
using System.Collections;
using System.Collections.Generic;
using Bpf.Controls.Routing;
using Bpf.Data;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 下拉选择框。点击展开列表(直接在下方渲染 ListBox),选择后收起。
    /// M4.1 简化:弹出层直接在 ComboBox 下方渲染(非独立 popup 窗口)。
    /// </summary>
    public sealed class ComboBox : Control, IPanel
    {
        private IEnumerable? _itemsSource;
        private readonly List<string> _itemTexts = new List<string>();
        private object? _selectedItem;
        private int _selectedIndex = -1;
        private bool _isDropdownOpen;

        // ── 属性 ──

        public static readonly StyledProperty<double> FontSizeProperty =
            StyledProperty<double>.Register<ComboBox>(nameof(FontSize), 14.0,
                affectsMeasure: true, affectsRender: true);

        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly StyledProperty<string> FontFamilyProperty =
            StyledProperty<string>.Register<ComboBox>(nameof(FontFamily), "Segoe UI",
                affectsMeasure: true, affectsRender: true);

        public string FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly StyledProperty<Brush> BackgroundProperty =
            StyledProperty<Brush>.Register<ComboBox>(nameof(Background),
                new SolidColorBrush(Color.White), affectsRender: true);

        public Brush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly StyledProperty<Brush> BorderBrushProperty =
            StyledProperty<Brush>.Register<ComboBox>(nameof(BorderBrush),
                new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), affectsRender: true);

        public Brush BorderBrush
        {
            get => GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public static readonly StyledProperty<Brush> SelectedBackgroundProperty =
            StyledProperty<Brush>.Register<ComboBox>(nameof(SelectedBackground),
                new SolidColorBrush(Color.FromRgb(0x2D, 0x8C, 0xFF)), affectsRender: true);

        public Brush SelectedBackground
        {
            get => GetValue(SelectedBackgroundProperty);
            set => SetValue(SelectedBackgroundProperty, value);
        }

        /// <summary>数据源集合。</summary>
        public IEnumerable? ItemsSource
        {
            get => _itemsSource;
            set
            {
                _itemsSource = value;
                RebuildTexts();
                InvalidateMeasure();
            }
        }

        /// <summary>当前选中项。</summary>
        public object? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                _selectedIndex = FindIndex(value);
                RaiseEvent(SelectionChangedEvent, new RoutedEventArgs());
                InvalidateVisual();
            }
        }

        /// <summary>当前选中索引。</summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;
                _selectedItem = (value >= 0 && value < _itemTexts.Count)
                    ? GetItemAt(value) : null;
                RaiseEvent(SelectionChangedEvent, new RoutedEventArgs());
                InvalidateVisual();
            }
        }

        // ── 路由事件 ──

        public static readonly RoutedEvent<RoutedEventArgs> SelectionChangedEvent =
            RoutedEvent<RoutedEventArgs>.Register<ComboBox>(nameof(SelectionChanged), RoutingStrategies.Bubble);

        public event EventHandler<RoutedEventArgs>? SelectionChanged
        {
            add => AddHandler(SelectionChangedEvent, value!);
            remove => RemoveHandler(SelectionChangedEvent, value!);
        }

        // ── IPanel(无实际子控件,ComboBox 自渲染列表项) ──

        IReadOnlyList<Control> IPanel.Children => Array.Empty<Control>();
        void IPanel.AddChild(Control child) { }
        void IPanel.RemoveChild(Control child) { }

        // ── 缓存文本格式 ──

        private IPlatformTextFormat? _format;

        public ComboBox()
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
                FontFamily, FontSize, FontWeight.Normal);
        }

        private void RebuildTexts()
        {
            _itemTexts.Clear();
            if (_itemsSource is null) return;
            foreach (var item in _itemsSource)
                _itemTexts.Add(item?.ToString() ?? "");
        }

        private object? GetItemAt(int index)
        {
            if (_itemsSource is null) return null;
            int idx = 0;
            foreach (var it in _itemsSource)
            {
                if (idx == index) return it;
                idx++;
            }
            return null;
        }

        private int FindIndex(object? item)
        {
            if (item is null || _itemsSource is null) return -1;
            int idx = 0;
            foreach (var it in _itemsSource)
            {
                if (ReferenceEquals(it, item)) return idx;
                idx++;
            }
            return -1;
        }

        // ── 布局 ──

        private const double ItemHeight = 24;
        private const double ComboHeight = 28;
        private const int MaxDropdownItems = 8;

        protected override Size MeasureCore(Size availableSize)
        {
            double width = availableSize.Width == double.PositiveInfinity ? 150 : availableSize.Width;
            double height = ComboHeight;
            if (_isDropdownOpen)
            {
                int visibleItems = Math.Min(_itemTexts.Count, MaxDropdownItems);
                height += visibleItems * ItemHeight;
            }
            return new Size(width, height);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = new Rect(finalRect.X, finalRect.Y, finalRect.Width, MeasureHeight());
        }

        private double MeasureHeight()
        {
            double h = ComboHeight;
            if (_isDropdownOpen)
            {
                int visibleItems = Math.Min(_itemTexts.Count, MaxDropdownItems);
                h += visibleItems * ItemHeight;
            }
            return h;
        }

        // ── 渲染 ──

        public override void Render(IDrawingContext context)
        {
            if (_format is null) RebuildFormat();
            var render = Bpf.Application.Application.Current.RenderInterface;
            double w = Bounds.Width;

            // 主体(选择框)
            var bg = Background.ToPlatform(render);
            var border = BorderBrush.ToPlatform(render);
            try
            {
                context.FillRectangle(new Rect(0, 0, w, ComboHeight), bg);
                context.DrawRectangle(new Rect(0.5, 0.5, w - 1, ComboHeight - 1), border, 1.0);
            }
            finally { bg.Dispose(); border.Dispose(); }

            // 当前选中文字
            string displayText = (_selectedIndex >= 0 && _selectedIndex < _itemTexts.Count)
                ? _itemTexts[_selectedIndex] : "";
            if (!string.IsNullOrEmpty(displayText) && _format is not null)
            {
                var fg = new SolidColorBrush(Color.Black).ToPlatform(render);
                try { context.DrawText(new Point(6, 5), displayText, _format, fg); }
                finally { fg.Dispose(); }
            }

            // 下拉箭头(用文字 ▼)
            if (_format is not null)
            {
                var arrowFg = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)).ToPlatform(render);
                try { context.DrawText(new Point(w - 20, 5), "▼", _format, arrowFg); }
                finally { arrowFg.Dispose(); }
            }

            // 展开时画下拉列表
            if (_isDropdownOpen)
            {
                double listY = ComboHeight;
                int visibleItems = Math.Min(_itemTexts.Count, MaxDropdownItems);

                var listBg = Background.ToPlatform(render);
                var listBorder = border;
                try
                {
                    // 列表背景+边框
                    var listRect = new Rect(0, listY, w, visibleItems * ItemHeight);
                    context.FillRectangle(listRect, listBg);
                    context.DrawRectangle(
                        new Rect(0.5, listY + 0.5, w - 1, visibleItems * ItemHeight - 1),
                        listBorder, 1.0);
                }
                finally { listBg.Dispose(); }

                // 列表项
                for (int i = 0; i < visibleItems; i++)
                {
                    double itemY = listY + i * ItemHeight;
                    bool isSelected = (i == _selectedIndex);
                    bool isHover = (i == _hoverIndex);

                    if (isSelected || isHover)
                    {
                        var itemBg = (isSelected ? SelectedBackground
                            : new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0))).ToPlatform(render);
                        try { context.FillRectangle(new Rect(1, itemY, w - 2, ItemHeight), itemBg); }
                        finally { itemBg.Dispose(); }
                    }

                    if (_format is not null && i < _itemTexts.Count)
                    {
                        var itemFg = (isSelected
                            ? new SolidColorBrush(Color.White)
                            : new SolidColorBrush(Color.Black)).ToPlatform(render);
                        try { context.DrawText(new Point(6, itemY + 2), _itemTexts[i], _format, itemFg); }
                        finally { itemFg.Dispose(); }
                    }
                }
            }
        }

        private int _hoverIndex = -1;

        // ── 输入 ──

        public override void OnPointerPressed(PointerEventArgs e)
        {
            double y = e.Position.Y;
            if (y <= ComboHeight)
            {
                // 点击主体:切换展开/收起
                _isDropdownOpen = !_isDropdownOpen;
                RenderOnTop = _isDropdownOpen;
                InvalidateMeasure();
                InvalidateVisual();
            }
            else if (_isDropdownOpen)
            {
                // 点击列表项:选中并收起
                int itemIdx = (int)((y - ComboHeight) / ItemHeight);
                if (itemIdx >= 0 && itemIdx < _itemTexts.Count)
                {
                    SelectedIndex = itemIdx;
                }
                _isDropdownOpen = false;
                RenderOnTop = false;
                InvalidateMeasure();
                InvalidateVisual();
            }
            e.Handled = true;
        }

        public override void OnPointerMoved(PointerEventArgs e)
        {
            if (_isDropdownOpen)
            {
                double y = e.Position.Y;
                int newHover = (y > ComboHeight)
                    ? (int)((y - ComboHeight) / ItemHeight) : -1;
                if (newHover >= _itemTexts.Count) newHover = -1;
                if (newHover != _hoverIndex)
                {
                    _hoverIndex = newHover;
                    InvalidateVisual();
                }
            }
        }

        public override bool HitTest(Point point)
        {
            return IsVisible && Bounds.Contains(point);
        }
    }
}
