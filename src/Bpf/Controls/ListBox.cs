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
    /// 列表选择控件。ItemsSource 绑定数据集合(推荐 ObservableCollection),
    /// SelectedItem/SelectedIndex 表示当前选择。点击项切换选择。
    /// M4 不含滚动(固定高度可见区),ScrollViewer 留 M4.1。
    /// </summary>
    public sealed class ListBox : Control, IPanel
    {
        private readonly List<ListBoxItem> _itemContainers = new List<ListBoxItem>();
        private IEnumerable? _itemsSource;
        private object? _selectedItem;
        private int _selectedIndex = -1;
        // 数据项的属性变化订阅(当数据项实现 INotifyPropertyChanged 时)
        private readonly List<(INotifyPropertyChanged item, PropertyChangedEventHandler handler)> _itemHandlers
            = new();

        // ── 属性 ──

        public static readonly StyledProperty<double> FontSizeProperty =
            StyledProperty<double>.Register<ListBox>(nameof(FontSize), 14.0,
                affectsMeasure: true, affectsRender: true);

        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly StyledProperty<string> FontFamilyProperty =
            StyledProperty<string>.Register<ListBox>(nameof(FontFamily), "Segoe UI",
                affectsMeasure: true, affectsRender: true);

        public string FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly StyledProperty<Brush> SelectedBackgroundProperty =
            StyledProperty<Brush>.Register<ListBox>(nameof(SelectedBackground),
                new SolidColorBrush(Color.FromRgb(0x2D, 0x8C, 0xFF)), affectsRender: true);

        public Brush SelectedBackground
        {
            get => GetValue(SelectedBackgroundProperty);
            set => SetValue(SelectedBackgroundProperty, value);
        }

        public static readonly StyledProperty<Brush> SelectedForegroundProperty =
            StyledProperty<Brush>.Register<ListBox>(nameof(SelectedForeground),
                new SolidColorBrush(Color.White), affectsRender: true);

        public Brush SelectedForeground
        {
            get => GetValue(SelectedForegroundProperty);
            set => SetValue(SelectedForegroundProperty, value);
        }

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<ListBox>(nameof(Foreground),
                new SolidColorBrush(Color.Black), affectsRender: true);

        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        /// <summary>数据源集合。</summary>
        public IEnumerable? ItemsSource
        {
            get => _itemsSource;
            set
            {
                // 解绑旧的
                if (_itemsSource is INotifyCollectionChanged oldNcc)
                    oldNcc.CollectionChanged -= OnCollectionChanged;

                _itemsSource = value;

                // 绑新的
                if (_itemsSource is INotifyCollectionChanged newNcc)
                    newNcc.CollectionChanged += OnCollectionChanged;

                RebuildItems();
                InvalidateMeasure();
            }
        }

        /// <summary>当前选中项(数据对象)。</summary>
        public object? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (!ReferenceEquals(_selectedItem, value))
                {
                    _selectedItem = value;
                    _selectedIndex = FindIndex(value);
                    UpdateItemSelection();
                    RaiseEvent(SelectionChangedEvent, new RoutedEventArgs());
                }
            }
        }

        /// <summary>当前选中索引(-1 = 无选中)。</summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    _selectedItem = (value >= 0 && value < _itemContainers.Count)
                        ? GetItemAt(value) : null;
                    UpdateItemSelection();
                    RaiseEvent(SelectionChangedEvent, new RoutedEventArgs());
                }
            }
        }

        // ── 路由事件 ──

        public static readonly RoutedEvent<RoutedEventArgs> SelectionChangedEvent =
            RoutedEvent<RoutedEventArgs>.Register<ListBox>(nameof(SelectionChanged), RoutingStrategies.Bubble);

        public event EventHandler<RoutedEventArgs>? SelectionChanged
        {
            add => AddHandler(SelectionChangedEvent, value!);
            remove => RemoveHandler(SelectionChangedEvent, value!);
        }

        // ── IPanel(ListBoxItem 作为子控件) ──

        IReadOnlyList<Control> IPanel.Children
        {
            get
            {
                // 返回一个 Control 列表视图
                var list = new List<Control>(_itemContainers.Count);
                foreach (var c in _itemContainers) list.Add(c);
                return list;
            }
        }

        void IPanel.AddChild(Control child) { /* ListBox 用 ItemsSource,不直接 AddChild */ }
        void IPanel.RemoveChild(Control child) { }

        // ── 构建/更新列表项 ──

        private void RebuildItems()
        {
            // 解绑旧数据项的属性通知
            foreach (var (item, handler) in _itemHandlers)
                item.PropertyChanged -= handler;
            _itemHandlers.Clear();

            _itemContainers.Clear();
            if (_itemsSource is null) return;

            int index = 0;
            foreach (var item in _itemsSource)
            {
                var container = new ListBoxItem
                {
                    Owner = this,
                    ItemIndex = index,
                    Content = item?.ToString() ?? "",
                };
                container.Parent = this;
                if (HostWindow is not null)
                    container.AttachToHost(HostWindow, logicalRoot: LogicalRoot);
                _itemContainers.Add(container);

                // 订阅数据项的属性变化,自动刷新文本
                if (item is INotifyPropertyChanged inpc)
                {
                    // 关键:复制到局部变量,避免闭包捕获循环变量(所有 lambda 会共享最终值)
                    int capturedIndex = index;
                    var capturedItem = item;
                    PropertyChangedEventHandler h = (s, e) =>
                    {
                        if (capturedIndex < _itemContainers.Count)
                        {
                            _itemContainers[capturedIndex].Content = capturedItem?.ToString() ?? "";
                            RequestRedraw();
                        }
                    };
                    inpc.PropertyChanged += h;
                    _itemHandlers.Add((inpc, h));
                }

                index++;
            }

            UpdateItemSelection();
        }

        /// <summary>
        /// 刷新所有列表项的显示文本(不重建容器,保留选中状态)。
        /// 数据对象的 ToString() 可能变化时调用。
        /// </summary>
        public void RefreshItems()
        {
            if (_itemsSource is null) return;
            int index = 0;
            foreach (var item in _itemsSource)
            {
                if (index < _itemContainers.Count)
                {
                    _itemContainers[index].Content = item?.ToString() ?? "";
                }
                index++;
            }
            RequestRedraw();
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildItems();
            InvalidateMeasure();
        }

        private void UpdateItemSelection()
        {
            for (int i = 0; i < _itemContainers.Count; i++)
            {
                _itemContainers[i].IsSelected = (i == _selectedIndex);
            }
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

        internal void OnItemClicked(ListBoxItem item)
        {
            SelectedIndex = item.ItemIndex;
        }

        // ── 布局 ──

        private const double ItemHeight = 24;

        protected override Size MeasureCore(Size availableSize)
        {
            int count = _itemContainers.Count;
            double height = count * ItemHeight;
            double width = availableSize.Width == double.PositiveInfinity ? 150 : availableSize.Width;
            return new Size(width, height);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = finalRect;
            for (int i = 0; i < _itemContainers.Count; i++)
            {
                double y = finalRect.Y + i * ItemHeight;
                _itemContainers[i].Arrange(new Rect(
                    finalRect.X, y, finalRect.Width, ItemHeight));
            }
        }

        // ── 渲染:委托给各 ListBoxItem ──

        public override void Render(IDrawingContext context)
        {
            foreach (var item in _itemContainers)
            {
                if (!item.IsVisible) continue;
                context.PushTranslate(new Vector(item.Bounds.X - Bounds.X, item.Bounds.Y - Bounds.Y));
                try { item.Render(context); }
                finally { context.PopTransform(); }
            }
        }

        public override bool HitTest(Point point)
        {
            if (!IsVisible || !Bounds.Contains(point)) return false;
            for (int i = _itemContainers.Count - 1; i >= 0; i--)
            {
                if (_itemContainers[i].HitTest(point)) return true;
            }
            return false;
        }
    }

    /// <summary>ListBox 的单个列表项容器(内部控件)。</summary>
    internal sealed class ListBoxItem : Control
    {
        public ListBox? Owner { get; set; }
        public int ItemIndex { get; set; }

        private string _content = "";
        public string Content
        {
            get => _content;
            set { _content = value; }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                // 触发重绘(简化:直接调 internal)
                Owner?.RequestRedraw();
            }
        }

        private IPlatformTextFormat? _format;

        protected override void OnAttachedToHost()
        {
            base.OnAttachedToHost();
            RebuildFormat();
        }

        private void RebuildFormat()
        {
            if (HostWindow is null) return;
            var fontSize = Owner?.FontSize ?? 14;
            var fontFamily = Owner?.FontFamily ?? "Segoe UI";
            _format?.Dispose();
            _format = Bpf.Application.Application.Current.RenderInterface.CreateTextFormat(
                fontFamily, fontSize, FontWeight.Normal);
        }

        protected override Size MeasureCore(Size availableSize) =>
            new Size(availableSize.Width, 24);

        protected override void ArrangeCore(Rect finalRect) => Bounds = finalRect;

        public override void Render(IDrawingContext context)
        {
            if (Owner is null) return;
            var render = Bpf.Application.Application.Current.RenderInterface;

            // 背景
            var bg = (_isSelected ? Owner.SelectedBackground : new SolidColorBrush(Color.White))
                .ToPlatform(render);
            try { context.FillRectangle(new Rect(0, 0, Bounds.Width, Bounds.Height), bg); }
            finally { bg.Dispose(); }

            // 文字
            if (_format is null) RebuildFormat();
            if (_format is not null && !string.IsNullOrEmpty(_content))
            {
                var fg = (_isSelected ? Owner.SelectedForeground : Owner.Foreground).ToPlatform(render);
                try { context.DrawText(new Point(4, 2), _content, _format, fg); }
                finally { fg.Dispose(); }
            }
        }

        public override void OnPointerPressed(PointerEventArgs e)
        {
            Owner?.OnItemClicked(this);
            e.Handled = true;
        }

        public override bool HitTest(Point point) =>
            IsVisible && Bounds.Contains(point);
    }
}
