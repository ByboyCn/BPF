using System;
using System.Collections.Generic;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 绝对定位面板。子控件通过 Canvas.SetLeft/SetTop 指定坐标,ZIndex 控制层叠顺序。
    /// Canvas 自身的 DesiredSize 为 0(不占用父布局空间),子控件在父坐标系内自由摆放。
    /// </summary>
    public sealed class Canvas : Control, IPanel
    {
        private readonly List<Control> _children = new List<Control>();

        // ── 附加属性 ──

        public static readonly AttachedProperty<double> LeftProperty =
            AttachedProperty<double>.Register<Canvas>("Left", 0, affectsArrange: true);

        public static readonly AttachedProperty<double> TopProperty =
            AttachedProperty<double>.Register<Canvas>("Top", 0, affectsArrange: true);

        public static readonly AttachedProperty<int> ZIndexProperty =
            AttachedProperty<int>.Register<Canvas>("ZIndex", 0, affectsArrange: true);

        public static double GetLeft(Control e) => e.GetValue(LeftProperty);
        public static void SetLeft(Control e, double value) => e.SetValue(LeftProperty, value);

        public static double GetTop(Control e) => e.GetValue(TopProperty);
        public static void SetTop(Control e, double value) => e.SetValue(TopProperty, value);

        public static int GetZIndex(Control e) => e.GetValue(ZIndexProperty);
        public static void SetZIndex(Control e, int value) => e.SetValue(ZIndexProperty, value);

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

        // ── 布局:子控件自由扩展,Canvas 不占用空间 ──

        protected override Size MeasureCore(Size availableSize)
        {
            foreach (var child in _children)
            {
                if (!child.IsVisible) continue;
                // 给子控件无限空间(Canvas 不约束子控件)
                child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }
            // Canvas 自身不占用父空间(DesiredSize=0)。其实际大小由父布局决定。
            return Size.Empty;
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = finalRect;
            foreach (var child in _children)
            {
                if (!child.IsVisible) continue;

                // 子控件坐标加上 finalRect 原点,使 Bounds 在父坐标系(与其它面板一致)
                double x = finalRect.X + GetLeft(child);
                double y = finalRect.Y + GetTop(child);
                var size = child.DesiredSize;
                child.Arrange(new Rect(x, y, size.Width, size.Height));
            }
        }

        // ── 渲染:按 ZIndex 排序后绘制(高 ZIndex 在上层) ──

        public override void Render(IDrawingContext context)
        {
            // 复制并按 ZIndex 升序排序(绘制顺序:低→高,高在上层)
            var sorted = _children.ToArray();
            Array.Sort(sorted, (a, b) => GetZIndex(a).CompareTo(GetZIndex(b)));

            foreach (var child in sorted)
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
            // 倒序遍历(高 ZIndex 在上层,优先命中)
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                if (_children[i].HitTest(point)) return true;
            }
            return false;
        }
    }
}
