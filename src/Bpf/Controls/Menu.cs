using System;
using System.Collections.Generic;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 菜单栏(顶部水平)。含若干 MenuItem,点击展开下拉子菜单。
    /// 子菜单项(LeafMenuItem)点击触发 Click 事件。
    /// </summary>
    public sealed class Menu : Control
    {
        private readonly List<MenuHeader> _headers = new List<MenuHeader>();
        private IPlatformTextFormat? _format;
        private int _openIndex = -1; // 当前展开的菜单索引
        private int _hoverHeader = -1;
        private int _hoverItem = -1;

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<Menu>(nameof(Foreground),
                new SolidColorBrush(Color.Black), affectsRender: true);
        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<Brush> BackgroundProperty =
            StyledProperty<Brush>.Register<Menu>(nameof(Background),
                new SolidColorBrush(Bpf.Theming.Theme.Current.WindowBackground), affectsRender: true);
        public Brush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        IReadOnlyList<MenuHeader> Headers => _headers;

        private const double MenuH = 24;
        private const double HeaderPad = 10;
        private const double ItemH = 22;
        private const double ItemPadX = 12;
        private const double DropdownMinW = 120;

        /// <summary>添加菜单头(如"文件")。</summary>
        public MenuHeader AddMenu(string header)
        {
            var h = new MenuHeader { Header = header, Owner = this, Index = _headers.Count };
            _headers.Add(h);
            InvalidateMeasure();
            return h;
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
                "Segoe UI", 13, FontWeight.Normal);
        }

        private IPlatformTextFormat EnsureFormat()
        {
            if (_format is null) RebuildFormat();
            return _format!;
        }

        protected override Size MeasureCore(Size availableSize)
        {
            double w = availableSize.Width == double.PositiveInfinity ? 200 : availableSize.Width;
            return new Size(w, MenuH);
        }

        protected override void ArrangeCore(Rect finalRect) => Bounds = finalRect;

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;
            var fmt = EnsureFormat();

            // 菜单栏背景
            var bg = Background.ToPlatform(render);
            try { context.FillRectangle(new Rect(0, 0, Bounds.Width, MenuH), bg); }
            finally { bg.Dispose(); }

            // 各菜单头
            double x = 0;
            var fg = Foreground.ToPlatform(render);
            var selBg = new SolidColorBrush(Bpf.Theming.Theme.Current.Selection).ToPlatform(render);
            var hoverBg = new SolidColorBrush(Bpf.Theming.Theme.Current.HoverBackground).ToPlatform(render);
            try
            {
                for (int i = 0; i < _headers.Count; i++)
                {
                    double hw = fmt.MeasureText(_headers[i].Header).Width + HeaderPad * 2;
                    bool open = i == _openIndex;
                    bool hovered = i == _hoverHeader;

                    if (open || hovered)
                    {
                        var hbg = (open ? selBg : hoverBg);
                        context.FillRectangle(new Rect(x, 0, hw, MenuH), hbg);
                    }
                    context.DrawText(new Point(x + HeaderPad, (MenuH - 13) / 2.0 - 1), _headers[i].Header, fmt,
                        open ? new SolidColorBrush(Bpf.Theming.Theme.Current.SelectionForeground).ToPlatform(render) : fg);
                    x += hw;
                }
            }
            finally { fg.Dispose(); selBg.Dispose(); hoverBg.Dispose(); }

            // 下拉菜单(展开时)
            if (_openIndex >= 0 && _openIndex < _headers.Count)
            {
                var header = _headers[_openIndex];
                double dropX = GetHeaderX(fmt, _openIndex);
                double dropW = ComputeDropdownWidth(fmt, header);
                context.PushTranslate(new Vector(dropX, MenuH));
                try { RenderDropdown(context, render, fmt, header, dropW); }
                finally { context.PopTransform(); }
            }
        }

        private void RenderDropdown(IDrawingContext context, IPlatformRenderInterface render,
            IPlatformTextFormat fmt, MenuHeader header, double dropW)
        {
            double dropH = header.Items.Count * ItemH;
            // 背景
            var bg = new SolidColorBrush(Color.White).ToPlatform(render);
            var border = new SolidColorBrush(Bpf.Theming.Theme.Current.Border).ToPlatform(render);
            try
            {
                context.FillRectangle(new Rect(0, 0, dropW, dropH), bg);
                context.DrawRectangle(new Rect(0.5, 0.5, dropW - 1, dropH - 1), border, 1.0);
            }
            finally { bg.Dispose(); border.Dispose(); }

            var fg = Foreground.ToPlatform(render);
            var hoverBg = new SolidColorBrush(Bpf.Theming.Theme.Current.HoverBackground).ToPlatform(render);
            var selFg = new SolidColorBrush(Bpf.Theming.Theme.Current.SelectionForeground).ToPlatform(render);
            var selBg = new SolidColorBrush(Bpf.Theming.Theme.Current.Selection).ToPlatform(render);
            try
            {
                for (int i = 0; i < header.Items.Count; i++)
                {
                    double y = i * ItemH;
                    bool hovered = i == _hoverItem;
                    if (hovered) context.FillRectangle(new Rect(0, y, dropW, ItemH), selBg);
                    context.DrawText(new Point(ItemPadX, y + (ItemH - 13) / 2.0 - 1), header.Items[i].Header, fmt,
                        hovered ? selFg : fg);
                }
            }
            finally { fg.Dispose(); hoverBg.Dispose(); selFg.Dispose(); selBg.Dispose(); }
        }

        private double GetHeaderX(IPlatformTextFormat fmt, int index)
        {
            double x = 0;
            for (int i = 0; i < index; i++)
                x += fmt.MeasureText(_headers[i].Header).Width + HeaderPad * 2;
            return x;
        }

        private double ComputeDropdownWidth(IPlatformTextFormat fmt, MenuHeader header)
        {
            double max = DropdownMinW;
            foreach (var item in header.Items)
                max = Math.Max(max, fmt.MeasureText(item.Header).Width + ItemPadX * 2 + 20);
            return max;
        }

        // ── 输入 ──

        public override void OnPointerPressed(PointerEventArgs e)
        {
            var fmt = EnsureFormat();
            // 点击菜单头
            double x = 0;
            for (int i = 0; i < _headers.Count; i++)
            {
                double hw = fmt.MeasureText(_headers[i].Header).Width + HeaderPad * 2;
                if (e.Position.X >= x && e.Position.X < x + hw && e.Position.Y < MenuH)
                {
                    _openIndex = (_openIndex == i) ? -1 : i; // 切换展开
                    _hoverItem = -1;
                    RenderOnTop = _openIndex >= 0; // 展开时浮在最上层
                    InvalidateVisual();
                    e.Handled = true;
                    return;
                }
                x += hw;
            }
            // 点击下拉项
            if (_openIndex >= 0 && e.Position.Y >= MenuH)
            {
                HandleItemClick(e.Position);
            }
        }

        private void HandleItemClick(Point pos)
        {
            var fmt = EnsureFormat();
            var header = _headers[_openIndex];
            double dropX = GetHeaderX(fmt, _openIndex);
            double dropW = ComputeDropdownWidth(fmt, header);
            double relX = pos.X - dropX;
            double relY = pos.Y - MenuH;
            if (relX >= 0 && relX < dropW)
            {
                int itemIdx = (int)(relY / ItemH);
                if (itemIdx >= 0 && itemIdx < header.Items.Count)
                {
                    header.Items[itemIdx].RaiseClick();
                    _openIndex = -1; // 点击后收起
                    RenderOnTop = false;
                    InvalidateVisual();
                }
            }
        }

        public override void OnPointerMoved(PointerEventArgs e)
        {
            var fmt = EnsureFormat();
            double x = 0;
            int newHover = -1;
            if (e.Position.Y < MenuH)
            {
                for (int i = 0; i < _headers.Count; i++)
                {
                    double hw = fmt.MeasureText(_headers[i].Header).Width + HeaderPad * 2;
                    if (e.Position.X >= x && e.Position.X < x + hw) { newHover = i; break; }
                    x += hw;
                }
            }
            if (newHover != _hoverHeader) { _hoverHeader = newHover; InvalidateVisual(); }

            // 下拉项悬停
            if (_openIndex >= 0 && e.Position.Y >= MenuH)
            {
                var header = _headers[_openIndex];
                double dropX = GetHeaderX(fmt, _openIndex);
                double relY = e.Position.Y - MenuH;
                double relX = e.Position.X - dropX;
                double dropW = ComputeDropdownWidth(fmt, header);
                int newItem = -1;
                if (relX >= 0 && relX < dropW) newItem = (int)(relY / ItemH);
                if (newItem != _hoverItem) { _hoverItem = newItem; InvalidateVisual(); }
            }
        }

        protected internal override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            if (_hoverHeader != -1) { _hoverHeader = -1; InvalidateVisual(); }
        }

        public override bool HitTest(Point point) => IsVisible && Bounds.Contains(point);
    }

    /// <summary>菜单头(如"文件"),含子菜单项列表。</summary>
    public sealed class MenuHeader
    {
        public string Header { get; set; } = "";
        public List<MenuItem> Items { get; } = new List<MenuItem>();
        internal Menu? Owner { get; set; }
        internal int Index { get; set; }

        /// <summary>添加菜单项。</summary>
        public MenuItem AddItem(string header)
        {
            var item = new MenuItem { Header = header };
            Items.Add(item);
            return item;
        }
    }

    /// <summary>菜单项(叶子节点)。点击触发 Click。</summary>
    public sealed class MenuItem
    {
        public string Header { get; set; } = "";
        public event EventHandler? Click;
        internal void RaiseClick() => Click?.Invoke(this, EventArgs.Empty);
    }
}
