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
                    InvalidateArrange();
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

        /// <summary>标签栏位置:Top(顶部横向,默认)或 Left(左侧竖向)。</summary>
        public Dock TabStripPlacement { get; set; } = Dock.Top;

        private const double TabH = 26;
        private const double TabMinW = 60;
        private const double TabMinH = 26;  // 竖向时每个标签的最小高度
        private const double TabStripW = 120; // 竖向标签栏宽度
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

        /// <summary>bpfaml 的直接子元素当作页面。标题默认 "Tab N",可用 SetPageHeader 修改。</summary>
        public void AddChild(Control child)
        {
            AddPage("Tab " + (_pages.Count + 1), child);
        }

        /// <summary>设置指定页的标题(用于代码里改 .bpfaml 自动生成的标签名)。</summary>
        public void SetPageHeader(int index, string header)
        {
            if (index >= 0 && index < _pages.Count)
            {
                _pages[index].Header = header;
                InvalidateVisual();
            }
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

        private bool IsVertical => TabStripPlacement == Dock.Left;

        /// <summary>标签栏占的宽度/高度(竖向=TabStripW,横向=TabH)。</summary>
        private double StripThickness => IsVertical ? TabStripW : TabH;

        protected override Size MeasureCore(Size availableSize)
        {
            double w = availableSize.Width == double.PositiveInfinity ? 400 : availableSize.Width;
            double h = availableSize.Height == double.PositiveInfinity ? 300 : availableSize.Height;
            // 内容区尺寸
            double contentW = IsVertical ? Math.Max(0, w - TabStripW) : w;
            double contentH = IsVertical ? h : Math.Max(0, h - TabH);

            if (_selectedIndex >= 0 && _selectedIndex < _pages.Count)
            {
                var content = _pages[_selectedIndex].Content;
                if (content is not null && content.IsVisible)
                {
                    content.Measure(new Size(contentW == 0 ? double.PositiveInfinity : contentW,
                                              contentH == 0 ? double.PositiveInfinity : contentH));
                }
            }
            return new Size(w, h);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = finalRect;
            double contentW, contentH, contentX, contentY;
            if (IsVertical)
            {
                contentX = finalRect.X + TabStripW;
                contentY = finalRect.Y;
                contentW = Math.Max(0, finalRect.Width - TabStripW);
                contentH = finalRect.Height;
            }
            else
            {
                contentX = finalRect.X;
                contentY = finalRect.Y + TabH;
                contentW = finalRect.Width;
                contentH = Math.Max(0, finalRect.Height - TabH);
            }
            if (_selectedIndex >= 0 && _selectedIndex < _pages.Count)
            {
                var content = _pages[_selectedIndex].Content;
                content?.Arrange(new Rect(contentX, contentY, contentW, contentH));
            }
        }

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;
            var fmt = EnsureFormat();
            bool vertical = IsVertical;

            // 标签栏背景
            var tabBg = new SolidColorBrush(Bpf.Theming.Theme.Current.WindowBackground).ToPlatform(render);
            try
            {
                if (vertical)
                    context.FillRectangle(new Rect(0, 0, TabStripW, Bounds.Height), tabBg);
                else
                    context.FillRectangle(new Rect(0, 0, Bounds.Width, TabH), tabBg);
            }
            finally { tabBg.Dispose(); }

            // 各标签
            var fg = Foreground.ToPlatform(render);
            var accent = new SolidColorBrush(Bpf.Theming.Theme.Current.Accent).ToPlatform(render);
            try
            {
                if (vertical)
                {
                    double y = 0;
                    for (int i = 0; i < _pages.Count; i++)
                    {
                        var page = _pages[i];
                        double th = Math.Max(TabMinH, fmt.MeasureText(page.Header).Height + TabPad);
                        RenderTabVertical(context, render, fmt, page.Header, 0, y, TabStripW, th, i == _selectedIndex, i == _hoverTab, fg, accent);
                        y += th;
                    }
                    // 标签栏右分隔线
                    var line = BorderBrush.ToPlatform(render);
                    try { context.FillRectangle(new Rect(TabStripW, 0, 1, Bounds.Height), line); }
                    finally { line.Dispose(); }
                }
                else
                {
                    double x = 0;
                    for (int i = 0; i < _pages.Count; i++)
                    {
                        var page = _pages[i];
                        double tw = MeasureTabWidth(page.Header, fmt);
                        RenderTabHorizontal(context, render, fmt, page.Header, x, 0, tw, TabH, i == _selectedIndex, i == _hoverTab, fg, accent);
                        x += tw;
                    }
                    // 标签栏底线
                    var line = BorderBrush.ToPlatform(render);
                    try { context.FillRectangle(new Rect(0, TabH, Bounds.Width, 1), line); }
                    finally { line.Dispose(); }
                }
            }
            finally { fg.Dispose(); accent.Dispose(); }

            // 内容区边框
            var cb = BorderBrush.ToPlatform(render);
            try
            {
                if (vertical)
                    context.DrawRectangle(new Rect(TabStripW + 0.5, 0.5, Bounds.Width - TabStripW - 1, Bounds.Height - 1), cb, 1.0);
                else
                    context.DrawRectangle(new Rect(0.5, TabH + 0.5, Bounds.Width - 1, Bounds.Height - TabH - 1), cb, 1.0);
            }
            finally { cb.Dispose(); }

            // 渲染当前页内容
            if (_selectedIndex >= 0 && _selectedIndex < _pages.Count)
            {
                var content = _pages[_selectedIndex].Content;
                if (content is not null && content.IsVisible)
                {
                    var offset = vertical ? new Vector(TabStripW, 0) : new Vector(0, TabH);
                    context.PushTranslate(offset);
                    try { content.Render(context); }
                    finally { context.PopTransform(); }
                }
            }
        }

        private void RenderTabHorizontal(IDrawingContext ctx, IPlatformRenderInterface render,
            IPlatformTextFormat fmt, string header, double x, double y, double w, double h,
            bool selected, bool hovered, IPlatformBrush fg, IPlatformBrush accent)
        {
            var bg = selected ? Background.ToPlatform(render)
                    : (hovered ? new SolidColorBrush(Bpf.Theming.Theme.Current.HoverBackground).ToPlatform(render) : null);
            if (bg is not null) { try { ctx.FillRectangle(new Rect(x, y, w, h), bg); } finally { bg.Dispose(); } }
            if (selected) ctx.FillRectangle(new Rect(x, h - 2, w, 2), accent);
            ctx.DrawText(new Point(x + TabPad, (h - FontSize) / 2.0 - 1), header, fmt, fg);
        }

        private void RenderTabVertical(IDrawingContext ctx, IPlatformRenderInterface render,
            IPlatformTextFormat fmt, string header, double x, double y, double w, double h,
            bool selected, bool hovered, IPlatformBrush fg, IPlatformBrush accent)
        {
            var bg = selected ? Background.ToPlatform(render)
                    : (hovered ? new SolidColorBrush(Bpf.Theming.Theme.Current.HoverBackground).ToPlatform(render) : null);
            if (bg is not null) { try { ctx.FillRectangle(new Rect(x, y, w, h), bg); } finally { bg.Dispose(); } }
            // 选中:左侧强调色竖线
            if (selected) ctx.FillRectangle(new Rect(0, y, 3, h), accent);
            ctx.DrawText(new Point(TabPad, y + (h - FontSize) / 2.0 - 1), header, fmt, fg);
        }

        // ── 输入:点击标签切换 ──

        public override void OnPointerPressed(PointerEventArgs e)
        {
            int idx = HitTestTab(e.Position);
            if (idx >= 0) { SelectedIndex = idx; e.Handled = true; }
        }

        public override void OnPointerMoved(PointerEventArgs e)
        {
            int idx = HitTestTab(e.Position);
            if (idx != _hoverTab) { _hoverTab = idx; InvalidateVisual(); }
        }

        /// <summary>返回坐标所在的标签索引(-1 = 不在标签栏)。</summary>
        private int HitTestTab(Point pos)
        {
            var fmt = EnsureFormat();
            if (IsVertical)
            {
                if (pos.X > TabStripW) return -1;
                double y = 0;
                for (int i = 0; i < _pages.Count; i++)
                {
                    double th = Math.Max(TabMinH, fmt.MeasureText(_pages[i].Header).Height + TabPad);
                    if (pos.Y >= y && pos.Y < y + th) return i;
                    y += th;
                }
            }
            else
            {
                if (pos.Y > TabH) return -1;
                double x = 0;
                for (int i = 0; i < _pages.Count; i++)
                {
                    double tw = MeasureTabWidth(_pages[i].Header, fmt);
                    if (pos.X >= x && pos.X < x + tw) return i;
                    x += tw;
                }
            }
            return -1;
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
