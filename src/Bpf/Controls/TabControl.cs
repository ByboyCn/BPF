using System;
using System.Collections.Generic;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 选项卡控件:多个页面用顶部标签切换。类似浏览器标签。
    /// TabPages 属性加页面(每个含 Header + Content),SelectedIndex 决定显示哪个页面的 Content。
    /// 标签栏在顶部,内容区在下方。点击标签切换。
    /// </summary>
    public sealed class TabControl : Control, IPanel
    {
        private readonly List<TabPage> _pages = new List<TabPage>();
        private readonly List<Control> _children = new List<Control>();
        private IPlatformTextFormat? _format;
        private int _selectedIndex = -1;
        private int _hoverTab = -1;

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<TabControl>(nameof(Foreground),
                new SolidColorBrush(Color.Black), affectsRender: true);
        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<Brush> BackgroundProperty =
            StyledProperty<Brush>.Register<TabControl>(nameof(Background),
                new SolidColorBrush(Color.White), affectsRender: true);
        public Brush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly StyledProperty<Brush> BorderBrushProperty =
            StyledProperty<Brush>.Register<TabControl>(nameof(BorderBrush),
                new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), affectsRender: true);
        public Brush BorderBrush
        {
            get => GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        IReadOnlyList<Control> IPanel.Children => _children;
        public IReadOnlyList<TabPage> Pages => _pages;

        /// <summary>当前选中的页面索引(-1 = 无)。</summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex != value)
                {
                    // 旧页隐藏(命中测试 + 渲染都跳过),新页显示
                    if (_selectedIndex >= 0 && _selectedIndex < _pages.Count)
                        _pages[_selectedIndex].Content!.IsVisible = false;
                    _selectedIndex = value;
                    if (_selectedIndex >= 0 && _selectedIndex < _pages.Count)
                        _pages[_selectedIndex].Content!.IsVisible = true;
                    InvalidateMeasure();
                    InvalidateVisual();
                }
            }
        }

        public static readonly StyledProperty<double> FontSizeProperty =
            StyledProperty<double>.Register<TabControl>(nameof(FontSize), 13.0,
                affectsMeasure: true, affectsRender: true);
        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        private const double TabH = 26;
        private const double TabMinW = 60;
        private const double TabPad = 12;

        /// <summary>添加一个页面。</summary>
        public void AddPage(string header, Control content)
        {
            var page = new TabPage { Header = header, Content = content };
            _pages.Add(page);
            _children.Add(content);
            content.Parent = this;
            // 非选中页隐藏(命中测试 + 渲染跳过)
            content.IsVisible = (_pages.Count == 1) || (_selectedIndex == _pages.Count - 1);
            if (HostWindow is not null)
                content.AttachToHost(HostWindow, logicalRoot: LogicalRoot);
            if (_selectedIndex < 0) { _selectedIndex = 0; content.IsVisible = true; }
            InvalidateMeasure();
        }

        /// <summary>bpfaml 的直接子元素当作页面(标题用序号)。也可在代码里调 AddPage 自定义标题。</summary>
        public void AddChild(Control child)
        {
            AddPage("Tab " + (_pages.Count + 1), child);
        }

        void IPanel.RemoveChild(Control child)
        {
            for (int i = _pages.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_pages[i].Content, child))
                {
                    _pages[i].Content.Parent = null;
                    _children.Remove(_pages[i].Content);
                    _pages.RemoveAt(i);
                    if (_selectedIndex >= _pages.Count) _selectedIndex = _pages.Count - 1;
                    InvalidateMeasure();
                    return;
                }
            }
        }

        protected override void OnAttachedToHost()
        {
            base.OnAttachedToHost();
            RebuildFormat();
        }

        protected override void AttachNonPanelChildren(IPlatformWindow window, Control logicalRoot)
        {
            foreach (var p in _pages)
                p.Content?.AttachToHost(window, logicalRoot);
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

        private double MeasureTabWidth(string header, IPlatformTextFormat fmt)
            => Math.Max(TabMinW, fmt.MeasureText(header).Width + TabPad * 2);

        protected override Size MeasureCore(Size availableSize)
        {
            double w = availableSize.Width == double.PositiveInfinity ? 200 : availableSize.Width;
            double h = TabH + 80; // 默认内容高度
            if (_selectedIndex >= 0 && _selectedIndex < _pages.Count)
            {
                var content = _pages[_selectedIndex].Content;
                if (content is not null && content.IsVisible)
                {
                    content.Measure(new Size(w, double.PositiveInfinity));
                    h = TabH + content.DesiredSize.Height;
                }
            }
            return new Size(w, h);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = finalRect;
            // 内容区:标签栏下方。给 content 的坐标是相对 TabControl 本地(0, TabH)。
            // 注意:content 的子会用 content.Bounds 累加,所以这里给的坐标会被继承。
            // Render 时用 PushTranslate(0, TabH) 移到内容区,content 从本地 (0,0) 画。
            double contentH = Math.Max(0, finalRect.Height - TabH);
            var contentRect = new Rect(finalRect.X, finalRect.Y + TabH, finalRect.Width, contentH);
            if (_selectedIndex >= 0 && _selectedIndex < _pages.Count)
            {
                var content = _pages[_selectedIndex].Content;
                content?.Arrange(contentRect);
            }
        }

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;
            var fmt = EnsureFormat();

            // 标签栏背景
            var tabBg = new SolidColorBrush(Bpf.Theming.Theme.Current.WindowBackground).ToPlatform(render);
            try { context.FillRectangle(new Rect(0, 0, Bounds.Width, TabH), tabBg); }
            finally { tabBg.Dispose(); }

            // 各标签
            double x = 0;
            for (int i = 0; i < _pages.Count; i++)
            {
                var page = _pages[i];
                double tw = MeasureTabWidth(page.Header, fmt);
                bool selected = i == _selectedIndex;
                bool hovered = i == _hoverTab;

                // 标签背景:选中=内容背景色,悬停=浅色,否则透明
                var bg = selected
                    ? Background.ToPlatform(render)
                    : (hovered ? new SolidColorBrush(Bpf.Theming.Theme.Current.HoverBackground).ToPlatform(render) : null);
                if (bg is not null)
                {
                    try { context.FillRectangle(new Rect(x, 0, tw, TabH), bg); }
                    finally { bg.Dispose(); }
                }

                // 标签底边线(选中时用强调色,否则边框色)
                var accent = new SolidColorBrush(Bpf.Theming.Theme.Current.Accent).ToPlatform(render);
                var border = BorderBrush.ToPlatform(render);
                try
                {
                    context.FillRectangle(new Rect(x, TabH - (selected ? 2 : 0), tw, selected ? 2 : 0), accent);
                }
                finally { accent.Dispose(); border.Dispose(); }

                // 标签文字
                var fg = Foreground.ToPlatform(render);
                try { context.DrawText(new Point(x + TabPad, (TabH - FontSize) / 2.0 - 1), page.Header, fmt, fg); }
                finally { fg.Dispose(); }

                x += tw;
            }

            // 标签栏底线
            var line = BorderBrush.ToPlatform(render);
            try { context.FillRectangle(new Rect(0, TabH, Bounds.Width, 1), line); }
            finally { line.Dispose(); }

            // 内容区边框
            var cb = BorderBrush.ToPlatform(render);
            try { context.DrawRectangle(new Rect(0.5, TabH + 0.5, Bounds.Width - 1, Bounds.Height - TabH - 1), cb, 1.0); }
            finally { cb.Dispose(); }

            // 渲染当前页内容:translate 到内容区(标签栏下方),content 从本地 (0,0) 画
            if (_selectedIndex >= 0 && _selectedIndex < _pages.Count)
            {
                var content = _pages[_selectedIndex].Content;
                if (content is not null && content.IsVisible)
                {
                    // 内容区起点:(0, TabH) 相对 TabControl 本地坐标
                    context.PushTranslate(new Vector(0, TabH));
                    try { content.Render(context); }
                    finally { context.PopTransform(); }
                }
            }
        }

        // ── 输入:点击标签切换 ──

        public override void OnPointerPressed(PointerEventArgs e)
        {
            if (e.Position.Y <= TabH)
            {
                var fmt = EnsureFormat();
                double x = 0;
                for (int i = 0; i < _pages.Count; i++)
                {
                    double tw = MeasureTabWidth(_pages[i].Header, fmt);
                    if (e.Position.X >= x && e.Position.X < x + tw)
                    {
                        SelectedIndex = i;
                        e.Handled = true;
                        return;
                    }
                    x += tw;
                }
            }
        }

        public override void OnPointerMoved(PointerEventArgs e)
        {
            if (e.Position.Y <= TabH)
            {
                var fmt = EnsureFormat();
                double x = 0;
                int newHover = -1;
                for (int i = 0; i < _pages.Count; i++)
                {
                    double tw = MeasureTabWidth(_pages[i].Header, fmt);
                    if (e.Position.X >= x && e.Position.X < x + tw) { newHover = i; break; }
                    x += tw;
                }
                if (newHover != _hoverTab) { _hoverTab = newHover; InvalidateVisual(); }
            }
            else if (_hoverTab != -1) { _hoverTab = -1; InvalidateVisual(); }
        }

        public override bool HitTest(Point point)
        {
            if (!IsVisible || !Bounds.Contains(point)) return false;
            return true;
        }
    }

    /// <summary>选项卡页面:标题 + 内容控件。</summary>
    public sealed class TabPage
    {
        public string Header { get; set; } = "";
        public Control? Content { get; set; }
    }
}
