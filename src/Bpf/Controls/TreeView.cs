using System;
using System.Collections.Generic;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 树形视图:层级数据,节点可展开/收起(类似文件资源管理器)。
    /// 用 ItemsSource(树节点列表)绑定数据,每个节点递归渲染。
    /// 点击节点的展开三角切换子节点可见性;点击节点文字选中。
    /// </summary>
    public sealed class TreeView : Control
    {
        private IPlatformTextFormat? _format;
        private readonly List<TreeNode> _roots = new List<TreeNode>();
        private TreeNode? _selected;

        public static readonly StyledProperty<Brush> ForegroundProperty =
            StyledProperty<Brush>.Register<TreeView>(nameof(Foreground),
                new SolidColorBrush(Color.Black), affectsRender: true);
        public Brush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<double> FontSizeProperty =
            StyledProperty<double>.Register<TreeView>(nameof(FontSize), 13.0,
                affectsMeasure: true, affectsRender: true);
        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>选中节点变化时触发。</summary>
        public event System.EventHandler<TreeNode>? SelectedNodeChanged;

        private const double Indent = 16;   // 每级缩进
        private const double NodeH = 22;    // 节点高度
        private const double TriX = 4;      // 三角距左
        private const double TriSize = 4;

        /// <summary>根节点列表。</summary>
        public IReadOnlyList<TreeNode> Roots => _roots;

        /// <summary>添加根节点。</summary>
        public void AddRoot(TreeNode node)
        {
            _roots.Add(node);
            node.Owner = this;
            InvalidateMeasure();
            InvalidateVisual();
        }

        /// <summary>清空。</summary>
        public void Clear()
        {
            foreach (var n in _roots) n.Owner = null;
            _roots.Clear();
            _selected = null;
            InvalidateMeasure();
            InvalidateVisual();
        }

        public TreeNode? SelectedNode
        {
            get => _selected;
            internal set
            {
                if (_selected != value)
                {
                    _selected = value;
                    SelectedNodeChanged?.Invoke(this, value!);
                    InvalidateVisual();
                }
            }
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
                "Segoe UI", FontSize, FontWeight.Normal);
        }

        private IPlatformTextFormat EnsureFormat()
        {
            if (_format is null) RebuildFormat();
            return _format!;
        }

        /// <summary>计算所有可见节点(展开的节点的子节点)的扁平列表,带缩进层级。</summary>
        private List<(TreeNode node, int depth)> Flatten()
        {
            var result = new List<(TreeNode, int)>();
            void Walk(TreeNode n, int depth)
            {
                result.Add((n, depth));
                if (n.IsExpanded)
                    foreach (var c in n.Children) Walk(c, depth + 1);
            }
            foreach (var r in _roots) Walk(r, 0);
            return result;
        }

        /// <summary>垂直滚动偏移(鼠标滚轮驱动)。</summary>
        private double _scrollOffset;

        protected override Size MeasureCore(Size availableSize)
        {
            var flat = Flatten();
            double w = availableSize.Width == double.PositiveInfinity ? 150 : availableSize.Width;
            double fullH = flat.Count * NodeH;
            // 尊重父给的高度约束(Height 属性)。无限高度时占全部内容。
            double h = availableSize.Height == double.PositiveInfinity ? fullH : Math.Min(fullH, availableSize.Height);
            return new Size(w, h);
        }

        protected override void ArrangeCore(Rect finalRect) => Bounds = finalRect;

        public override void Render(IDrawingContext context)
        {
            // 裁剪到 Bounds,防止内容穿透到其他控件
            context.PushClip(new Rect(0, 0, Bounds.Width, Bounds.Height));
            try
            {
                var render = Bpf.Application.Application.Current.RenderInterface;
                var fmt = EnsureFormat();
                var flat = Flatten();
                // 应用滚动偏移
                context.PushTranslate(new Vector(0, -_scrollOffset));
                RenderNodes(context, render, fmt, flat);
                context.PopTransform();
            }
            finally { context.PopClip(); }
        }

        private void RenderNodes(Platform.IDrawingContext context, Platform.IPlatformRenderInterface render,
            Platform.IPlatformTextFormat fmt, List<(TreeNode node, int depth)> flat)
        {

            var fg = Foreground.ToPlatform(render);
            var selBg = new SolidColorBrush(Bpf.Theming.Theme.Current.Selection).ToPlatform(render);
            var selFg = new SolidColorBrush(Bpf.Theming.Theme.Current.SelectionForeground).ToPlatform(render);
            var hoverBg = new SolidColorBrush(Bpf.Theming.Theme.Current.HoverBackground).ToPlatform(render);
            var arrow = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40)).ToPlatform(render);
            try
            {
                double y = 0;
                foreach (var (node, depth) in flat)
                {
                    double x = depth * Indent;
                    bool selected = node == _selected;
                    bool hovered = node.IsHovered;

                    // 行背景:选中 > 悬停 > 无
                    if (selected)
                        context.FillRectangle(new Rect(0, y, Bounds.Width, NodeH), selBg);
                    else if (hovered)
                        context.FillRectangle(new Rect(0, y, Bounds.Width, NodeH), hoverBg);

                    // 展开/收起三角(仅有子节点的才画)
                    if (node.Children.Count > 0)
                    {
                        double cx = x + TriX + TriSize;
                        double cy = y + NodeH / 2.0;
                        if (node.IsExpanded)
                            context.FillTriangle(new Point(cx - TriSize, cy - TriSize / 2), new Point(cx + TriSize, cy - TriSize / 2), new Point(cx, cy + TriSize), arrow);
                        else
                            context.FillTriangle(new Point(cx - TriSize / 2, cy - TriSize), new Point(cx - TriSize / 2, cy + TriSize), new Point(cx + TriSize, cy), arrow);
                    }

                    // 节点文字
                    double textX = x + TriX + TriSize * 2 + 6;
                    double textY = y + (NodeH - FontSize) / 2.0 - 1;
                    context.DrawText(new Point(textX, textY), node.Header, fmt, selected ? selFg : fg);

                    y += NodeH;
                }
            }
            finally
            {
                fg.Dispose(); selBg.Dispose(); selFg.Dispose(); hoverBg.Dispose(); arrow.Dispose();
            }
        }

        // ── 输入 ──

        public override void OnPointerPressed(PointerEventArgs e)
        {
            var flat = Flatten();
            // 点击 Y 需加回滚动偏移,找到实际节点
            double clickY = e.Position.Y + _scrollOffset;
            double y = 0;
            for (int i = 0; i < flat.Count; i++)
            {
                var (node, depth) = flat[i];
                double triX = depth * Indent + TriX;
                if (clickY >= y && clickY < y + NodeH)
                {
                    if (node.Children.Count > 0 && e.Position.X >= triX && e.Position.X < triX + TriSize * 3)
                    {
                        node.IsExpanded = !node.IsExpanded;
                        InvalidateMeasure();
                    }
                    else
                    {
                        SelectedNode = node;
                    }
                    InvalidateVisual();
                    e.Handled = true;
                    return;
                }
                y += NodeH;
            }
        }

        public override void OnPointerMoved(PointerEventArgs e)
        {
            var flat = Flatten();
            double clickY = e.Position.Y + _scrollOffset;
            double y = 0;
            bool changed = false;
            for (int i = 0; i < flat.Count; i++)
            {
                var (node, _) = flat[i];
                bool inRow = clickY >= y && clickY < y + NodeH;
                if (inRow != node.IsHovered) { node.IsHovered = inRow; changed = true; }
                y += NodeH;
            }
            if (changed) InvalidateVisual();
        }

        protected internal override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            var flat = Flatten();
            bool changed = false;
            foreach (var (node, _) in flat)
                if (node.IsHovered) { node.IsHovered = false; changed = true; }
            if (changed) InvalidateVisual();
        }

        /// <summary>鼠标滚轮:滚动树内容。</summary>
        protected internal override void OnMouseWheel(Bpf.Platform.MouseWheelEventArgs e)
        {
            double fullH = Flatten().Count * NodeH;
            double max = Math.Max(0, fullH - Bounds.Height);
            _scrollOffset = Math.Clamp(_scrollOffset - e.Delta / 120.0 * 48, 0, max);
            InvalidateVisual();
            e.Handled = true;
        }

        public override bool HitTest(Point point) => IsVisible && Bounds.Contains(point);
    }

    /// <summary>树节点。Header 是显示文字,Children 是子节点列表,IsExpanded 控制子节点可见。</summary>
    public sealed class TreeNode
    {
        public string Header { get; set; } = "";
        public List<TreeNode> Children { get; } = new List<TreeNode>();
        public bool IsExpanded { get; set; } = true;
        internal TreeView? Owner { get; set; }
        internal bool IsHovered { get; set; }

        /// <summary>添加子节点(便捷)。</summary>
        public TreeNode AddChild(string header)
        {
            var c = new TreeNode { Header = header, Owner = Owner };
            Children.Add(c);
            return c;
        }
    }
}
