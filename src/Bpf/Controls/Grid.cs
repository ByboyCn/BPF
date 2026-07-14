using System;
using System.Collections.Generic;
using Bpf.Layout;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 表格布局面板。子控件按行/列定位(通过附加属性 Grid.Row/Grid.Column),
    /// 行高/列宽支持 Auto/Pixel/Star 三种单位。支持 RowSpan/ColumnSpan。
    /// </summary>
    public sealed class Grid : Control, IPanel
    {
        private readonly List<Control> _children = new List<Control>();

        // ── 附加属性 ──

        /// <summary>子控件所在的行索引(默认 0)。</summary>
        public static readonly AttachedProperty<int> RowProperty =
            AttachedProperty<int>.Register<Grid>("Row", 0, affectsArrange: true);

        /// <summary>子控件所在的列索引(默认 0)。</summary>
        public static readonly AttachedProperty<int> ColumnProperty =
            AttachedProperty<int>.Register<Grid>("Column", 0, affectsArrange: true);

        /// <summary>子控件跨的行数(默认 1)。</summary>
        public static readonly AttachedProperty<int> RowSpanProperty =
            AttachedProperty<int>.Register<Grid>("RowSpan", 1, affectsArrange: true);

        /// <summary>子控件跨的列数(默认 1)。</summary>
        public static readonly AttachedProperty<int> ColumnSpanProperty =
            AttachedProperty<int>.Register<Grid>("ColumnSpan", 1, affectsArrange: true);

        public static int GetRow(Control element) => element.GetValue(RowProperty);
        public static void SetRow(Control element, int value) => element.SetValue(RowProperty, value);

        public static int GetColumn(Control element) => element.GetValue(ColumnProperty);
        public static void SetColumn(Control element, int value) => element.SetValue(ColumnProperty, value);

        public static int GetRowSpan(Control element) => element.GetValue(RowSpanProperty);
        public static void SetRowSpan(Control element, int value) => element.SetValue(RowSpanProperty, value);

        public static int GetColumnSpan(Control element) => element.GetValue(ColumnSpanProperty);
        public static void SetColumnSpan(Control element, int value) => element.SetValue(ColumnSpanProperty, value);

        // ── 行列定义 ──

        private GridLength[] _rowDefs = Array.Empty<GridLength>();
        private GridLength[] _colDefs = Array.Empty<GridLength>();

        /// <summary>行定义(字符串便捷形式,如 "auto,*,100")。设置后覆盖 RowDefinitions。</summary>
        public string Rows
        {
            get => string.Join(",", _rowDefs);
            set => _rowDefs = GridLength.ParseAll(value ?? "");
        }

        /// <summary>列定义(字符串便捷形式)。</summary>
        public string Columns
        {
            get => string.Join(",", _colDefs);
            set => _colDefs = GridLength.ParseAll(value ?? "");
        }

        /// <summary>行定义(对象形式)。设置后覆盖 Rows。</summary>
        public IReadOnlyList<GridLength> RowDefinitions
        {
            get => Array.AsReadOnly(_rowDefs);
            set => _rowDefs = value is null || value.Count == 0
                ? Array.Empty<GridLength>()
                : toArray(value);
        }

        /// <summary>列定义(对象形式)。</summary>
        public IReadOnlyList<GridLength> ColumnDefinitions
        {
            get => Array.AsReadOnly(_colDefs);
            set => _colDefs = value is null || value.Count == 0
                ? Array.Empty<GridLength>()
                : toArray(value);
        }

        private static GridLength[] toArray(IReadOnlyList<GridLength> src)
        {
            var arr = new GridLength[src.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = src[i];
            return arr;
        }

        // ── IPanel ──

        public IReadOnlyList<Control> Children => _children;

        public void AddChild(Control child)
        {
            _children.Add(child);
            child.Parent = this;
            if (HostWindow is not null)
                child.AttachToHost(HostWindow, logicalRoot: LogicalRoot);
            InvalidateMeasure();
        }

        public void RemoveChild(Control child)
        {
            if (_children.Remove(child))
            {
                child.Parent = null;
                InvalidateMeasure();
            }
        }

        // ── 布局:两阶段 Measure ──

        // 临时存储:measure 阶段计算出的每行/每列实际尺寸
        private double[] _rowSizes = Array.Empty<double>();
        private double[] _colSizes = Array.Empty<double>();

        protected override Size MeasureCore(Size availableSize)
        {
            int rowCount = Math.Max(1, _rowDefs.Length);
            int colCount = Math.Max(1, _colDefs.Length);

            // 默认:没有显式定义时,按子控件实际占用的行列自动扩展到 1 行 1 列
            // (即所有子控件默认在第 0 行第 0 列)
            EnsureDefs(rowCount, colCount);

            var rowSizes = new double[rowCount]; // 最终行高
            var colSizes = new double[colCount]; // 最终列宽

            // ── 第一阶段:测量子控件,收集 Auto 行列的最大需求 ──
            foreach (var child in _children)
            {
                if (!child.IsVisible) continue;

                int row = ClampIndex(GetRow(child), rowCount);
                int col = ClampIndex(GetColumn(child), colCount);
                int rowSpan = Math.Max(1, GetRowSpan(child));
                int colSpan = Math.Max(1, GetColumnSpan(child));

                // 子控件的可用空间:Star 视为无限(暂未分配),Pixel 用声明值,Auto 用无限
                var childAvail = ComputeChildAvailable(availableSize, row, col, rowSpan, colSpan);

                child.Measure(childAvail);
                var desired = child.DesiredSize;

                // Auto 行:取所有该行子控件的最大高度(考虑 Span 时按比例分配暂用最大)
                UpdateAutoSizes(rowSizes, colSizes, _rowDefs, _colDefs, row, col, rowSpan, colSpan, desired);
            }

            // ── 第二阶段:Pixel 行列固定,Star 按比例分配剩余 ──
            DistributeSizes(_rowDefs, rowSizes, availableSize.Height, isRow: true);
            DistributeSizes(_colDefs, colSizes, availableSize.Width, isRow: false);

            _rowSizes = rowSizes;
            _colSizes = colSizes;

            // Grid 总尺寸 = 所有行高之和 × 所有列宽之和
            double totalH = Sum(rowSizes);
            double totalW = Sum(colSizes);
            return new Size(
                Math.Min(availableSize.Width, totalW),
                Math.Min(availableSize.Height, totalH));
        }

        private void EnsureDefs(int rowCount, int colCount)
        {
            // 如果用户没定义行/列,补一个默认的 Auto(Avalonia:无定义=单格)
            if (_rowDefs.Length == 0) _rowDefs = new[] { GridLength.Auto };
            if (_colDefs.Length == 0) _colDefs = new[] { GridLength.Auto };
        }

        /// <summary>计算子控件的可用空间。Pixel 行/列给固定值,其余给无限(自由扩展)。</summary>
        private Size ComputeChildAvailable(Size available, int row, int col, int rowSpan, int colSpan)
        {
            double w = 0, h = 0;
            bool wInfinite = false, hInfinite = false;

            for (int c = col; c < col + colSpan && c < _colDefs.Length; c++)
            {
                var def = _colDefs[c];
                if (def.IsAbsolute) w += def.Value;
                else if (def.IsAuto || def.IsStar) { wInfinite = true; break; }
            }
            for (int r = row; r < row + rowSpan && r < _rowDefs.Length; r++)
            {
                var def = _rowDefs[r];
                if (def.IsAbsolute) h += def.Value;
                else if (def.IsAuto || def.IsStar) { hInfinite = true; break; }
            }

            return new Size(
                wInfinite ? available.Width : w,
                hInfinite ? available.Height : h);
        }

        /// <summary>第一阶段:更新 Auto 行/列的临时尺寸(取子控件 DesiredSize 的最大值)。</summary>
        private static void UpdateAutoSizes(
            double[] rowSizes, double[] colSizes,
            GridLength[] rowDefs, GridLength[] colDefs,
            int row, int col, int rowSpan, int colSpan, Size desired)
        {
            // Auto 行(单行时直接取;多行 Span 时把高度平均摊到各 Auto 行,简化处理取 max)
            if (rowSpan == 1 && row < rowDefs.Length && rowDefs[row].IsAuto)
                rowSizes[row] = Math.Max(rowSizes[row], desired.Height);

            if (colSpan == 1 && col < colDefs.Length && colDefs[col].IsAuto)
                colSizes[col] = Math.Max(colSizes[col], desired.Width);
        }

        /// <summary>第二阶段:Pixel 固定值,Star 按比例瓜分剩余空间。Auto 已在第一阶段确定。</summary>
        private static void DistributeSizes(GridLength[] defs, double[] sizes, double total, bool isRow)
        {
            // Pixel 行直接用声明值
            double used = 0;
            double starSum = 0;
            for (int i = 0; i < defs.Length; i++)
            {
                if (defs[i].IsAbsolute)
                {
                    sizes[i] = defs[i].Value;
                    used += defs[i].Value;
                }
                else if (defs[i].IsAuto)
                {
                    used += sizes[i]; // 第一阶段已填
                }
                else if (defs[i].IsStar)
                {
                    starSum += defs[i].Value;
                }
            }

            // Star 瓜分剩余
            double remaining = Math.Max(0, total - used);
            for (int i = 0; i < defs.Length; i++)
            {
                if (defs[i].IsStar && starSum > 0)
                {
                    sizes[i] = remaining * defs[i].Value / starSum;
                }
            }
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = finalRect;

            // 累计偏移量,用于定位每个单元格
            double[] rowOffsets = CumulativeOffsets(_rowSizes);
            double[] colOffsets = CumulativeOffsets(_colSizes);

            int rowCount = _rowSizes.Length;
            int colCount = _colSizes.Length;

            foreach (var child in _children)
            {
                if (!child.IsVisible) continue;

                int row = ClampIndex(GetRow(child), rowCount);
                int col = ClampIndex(GetColumn(child), colCount);
                int rowSpan = Math.Min(Math.Max(1, GetRowSpan(child)), rowCount - row);
                int colSpan = Math.Min(Math.Max(1, GetColumnSpan(child)), colCount - col);

                // 计算 child 占据的矩形(加上 finalRect 原点,使 Bounds 在父坐标系)
                double x = finalRect.X + colOffsets[col];
                double y = finalRect.Y + rowOffsets[row];
                double w = 0, h = 0;
                for (int c = col; c < col + colSpan; c++) w += _colSizes[c];
                for (int r = row; r < row + rowSpan; r++) h += _rowSizes[r];

                child.Arrange(new Rect(x, y, w, h));
            }
        }

        // ── 渲染:Grid 本身不画,依次绘制子控件(各在自己的 Bounds) ──

        public override void Render(IDrawingContext context)
        {
            foreach (var child in _children)
            {
                if (!child.IsVisible) continue;
                context.PushTranslate(new Vector(child.Bounds.X - Bounds.X, child.Bounds.Y - Bounds.Y));
                try
                {
                    child.Render(context);
                }
                finally
                {
                    context.PopTransform();
                }
            }
        }

        public override bool HitTest(Point point)
        {
            if (!IsVisible) return false;
            foreach (var child in _children)
            {
                if (child.HitTest(point)) return true;
            }
            return false;
        }

        // ── 辅助 ──

        private static int ClampIndex(int idx, int count) =>
            idx < 0 ? 0 : (idx >= count ? count - 1 : idx);

        private static double Sum(double[] arr)
        {
            double s = 0;
            for (int i = 0; i < arr.Length; i++) s += arr[i];
            return s;
        }

        private static double[] CumulativeOffsets(double[] sizes)
        {
            var offsets = new double[sizes.Length + 1];
            for (int i = 0; i < sizes.Length; i++)
                offsets[i + 1] = offsets[i] + sizes[i];
            return offsets;
        }
    }
}
