using System;
using System.Collections.Generic;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 数据表格:多列多行,带表头。类似 DataGrid / ListView 详情模式。
    /// 用 Columns 定义列(DataGridColumn),Rows 设数据(每行是字符串[] 或对象)。
    /// 单击行选中(蓝色高亮)。表头点击预留排序(暂不实现)。
    /// </summary>
    public sealed class DataGrid : Control
    {
        private readonly List<DataGridColumn> _columns = new List<DataGridColumn>();
        private List<string[]> _rows = new List<string[]>();
        private int _selectedIndex = -1;
        private int _hoverRow = -1;
        private int _hoverCol = -1;
        private IPlatformTextFormat? _format;
        private double _scrollOffset;

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<DataGrid>(nameof(Foreground),
                new SolidColorBrush(Color.Black), affectsRender: true);
        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<Brush> BackgroundProperty =
            StyledProperty<Brush>.Register<DataGrid>(nameof(Background),
                new SolidColorBrush(Color.White), affectsRender: true);
        public Brush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly StyledProperty<Brush> BorderBrushProperty =
            StyledProperty<Brush>.Register<DataGrid>(nameof(BorderBrush),
                new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), affectsRender: true);
        public Brush BorderBrush
        {
            get => GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public IReadOnlyList<DataGridColumn> Columns => _columns;

        /// <summary>选中行索引(-1 = 无)。</summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set { _selectedIndex = value; InvalidateVisual(); }
        }

        public event EventHandler? SelectionChanged;

        private const double HeaderH = 26;
        private const double RowH = 24;
        private const double CellPadX = 6;

        /// <summary>添加列。</summary>
        public void AddColumn(string header, double width = 0)
        {
            _columns.Add(new DataGridColumn { Header = header, Width = width });
            InvalidateMeasure();
        }

        /// <summary>设置数据行(每行是字符串数组,按列顺序)。</summary>
        public void SetRows(IEnumerable<string[]> rows)
        {
            _rows = new List<string[]>(rows);
            _selectedIndex = -1;
            InvalidateMeasure();
            InvalidateVisual();
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

        /// <summary>计算各列实际宽度(0 = 自适应内容)。</summary>
        private double[] ComputeWidths(IPlatformTextFormat fmt, double availableW)
        {
            var widths = new double[_columns.Count];
            double totalFixed = 0;
            int autoCount = 0;
            for (int c = 0; c < _columns.Count; c++)
            {
                if (_columns[c].Width > 0) { widths[c] = _columns[c].Width; totalFixed += _columns[c].Width; }
                else autoCount++;
            }
            // 自适应列平分剩余空间
            double autoW = autoCount > 0 ? Math.Max(60, (availableW - totalFixed) / autoCount) : 60;
            for (int c = 0; c < _columns.Count; c++)
                if (widths[c] == 0) widths[c] = autoW;
            return widths;
        }

        protected override Size MeasureCore(Size availableSize)
        {
            double w = availableSize.Width == double.PositiveInfinity ? 300 : availableSize.Width;
            double h = HeaderH + _rows.Count * RowH;
            double maxH = availableSize.Height == double.PositiveInfinity ? h : Math.Min(h, availableSize.Height);
            return new Size(w, maxH);
        }

        protected override void ArrangeCore(Rect finalRect) => Bounds = finalRect;

        public override void Render(IDrawingContext context)
        {
            var render = Bpf.Application.Application.Current.RenderInterface;
            var fmt = EnsureFormat();
            var widths = ComputeWidths(fmt, Bounds.Width);

            context.PushClip(new Rect(0, 0, Bounds.Width, Bounds.Height));
            try
            {
                // 背景
                var bg = Background.ToPlatform(render);
                try { context.FillRectangle(new Rect(0, 0, Bounds.Width, Bounds.Height), bg); }
                finally { bg.Dispose(); }

                context.PushTranslate(new Vector(0, -_scrollOffset));
                try
                {
                    // 表头
                    var headerBg = new SolidColorBrush(Bpf.Theming.Theme.Current.HoverBackground).ToPlatform(render);
                    var border = BorderBrush.ToPlatform(render);
                    var fg = Foreground.ToPlatform(render);
                    try
                    {
                        context.FillRectangle(new Rect(0, 0, Bounds.Width, HeaderH), headerBg);
                        double x = 0;
                        for (int c = 0; c < _columns.Count; c++)
                        {
                            context.DrawText(new Point(x + CellPadX, (HeaderH - 13) / 2.0 - 1), _columns[c].Header, fmt, fg);
                            // 列分隔线
                            context.FillRectangle(new Rect(x + widths[c], 0, 1, Bounds.Height), border);
                            x += widths[c];
                        }
                        // 表头底线
                        context.FillRectangle(new Rect(0, HeaderH, Bounds.Width, 1), border);
                    }
                    finally { headerBg.Dispose(); border.Dispose(); fg.Dispose(); }

                    // 行
                    var selBg = new SolidColorBrush(Bpf.Theming.Theme.Current.Selection).ToPlatform(render);
                    var selFg = new SolidColorBrush(Bpf.Theming.Theme.Current.SelectionForeground).ToPlatform(render);
                    var hoverBg = new SolidColorBrush(Bpf.Theming.Theme.Current.HoverBackground).ToPlatform(render);
                    var rowFg = Foreground.ToPlatform(render);
                    try
                    {
                        for (int r = 0; r < _rows.Count; r++)
                        {
                            double y = HeaderH + r * RowH;
                            bool selected = r == _selectedIndex;
                            bool hovered = r == _hoverRow;
                            if (selected)
                                context.FillRectangle(new Rect(0, y, Bounds.Width, RowH), selBg);
                            else if (hovered)
                                context.FillRectangle(new Rect(0, y, Bounds.Width, RowH), hoverBg);

                            double x = 0;
                            for (int c = 0; c < _columns.Count && c < _rows[r].Length; c++)
                            {
                                context.DrawText(new Point(x + CellPadX, y + (RowH - 13) / 2.0 - 1), _rows[r][c], fmt,
                                    selected ? selFg : rowFg);
                                context.FillRectangle(new Rect(x + widths[c], y, 1, RowH), BorderBrush.ToPlatform(render));
                                x += widths[c];
                            }
                            // 行底线
                            context.FillRectangle(new Rect(0, y + RowH, Bounds.Width, 1), BorderBrush.ToPlatform(render));
                        }
                    }
                    finally { selBg.Dispose(); selFg.Dispose(); hoverBg.Dispose(); rowFg.Dispose(); }
                }
                finally { context.PopTransform(); }
            }
            finally { context.PopClip(); }
        }

        // ── 输入 ──

        public override void OnPointerPressed(PointerEventArgs e)
        {
            double clickY = e.Position.Y + _scrollOffset - HeaderH;
            if (clickY < 0) return; // 表头
            int row = (int)(clickY / RowH);
            if (row >= 0 && row < _rows.Count)
            {
                _selectedIndex = row;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                InvalidateVisual();
            }
            e.Handled = true;
        }

        public override void OnPointerMoved(PointerEventArgs e)
        {
            double clickY = e.Position.Y + _scrollOffset - HeaderH;
            int row = (clickY >= 0) ? (int)(clickY / RowH) : -1;
            if (row != _hoverRow) { _hoverRow = row; InvalidateVisual(); }
        }

        protected internal override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            if (_hoverRow != -1) { _hoverRow = -1; InvalidateVisual(); }
        }

        protected internal override void OnMouseWheel(MouseWheelEventArgs e)
        {
            double fullH = HeaderH + _rows.Count * RowH;
            double max = Math.Max(0, fullH - Bounds.Height);
            _scrollOffset = Math.Clamp(_scrollOffset - e.Delta / 120.0 * 48, 0, max);
            InvalidateVisual();
            e.Handled = true;
        }

        public override bool HitTest(Point point) => IsVisible && Bounds.Contains(point);
    }

    /// <summary>数据表格列。</summary>
    public sealed class DataGridColumn
    {
        public string Header { get; set; } = "";
        /// <summary>列宽(0 = 自适应平分剩余空间)。</summary>
        public double Width { get; set; }
    }
}
