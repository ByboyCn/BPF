using System.Collections.Generic;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 简单堆叠面板。子控件按垂直或水平方向依次排列。
    /// </summary>
    public sealed class StackPanel : Control, IPanel
    {
        private readonly List<Control> _children = new List<Control>();

        public static readonly StyledProperty<Orientation> OrientationProperty =
            StyledProperty<Orientation>.Register<StackPanel>(
                nameof(Orientation), Orientation.Vertical,
                affectsMeasure: true, affectsArrange: true);

        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public IReadOnlyList<Control> Children => _children;

        public void AddChild(Control child)
        {
            _children.Add(child);
            child.Parent = this;
            // 若本面板已挂到窗口,立即把新子节点也 attach 进去,
            // 这样后添加的控件也能拿到 HostWindow(用于创建文本格式等平台资源)。
            if (HostWindow is not null)
            {
                child.AttachToHost(HostWindow, logicalRoot: LogicalRoot);
            }
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

        protected override Size MeasureCore(Size availableSize)
        {
            double width = 0, height = 0;
            var isVertical = Orientation == Orientation.Vertical;

            foreach (var child in _children)
            {
                if (!child.IsVisible) continue;

                // 给子控件"无限"在堆叠方向上的空间
                var childConstraint = isVertical
                    ? new Size(availableSize.Width, double.PositiveInfinity)
                    : new Size(double.PositiveInfinity, availableSize.Height);

                child.Measure(childConstraint);

                if (isVertical)
                {
                    width = System.Math.Max(width, child.DesiredSize.Width);
                    height += child.DesiredSize.Height;
                }
                else
                {
                    height = System.Math.Max(height, child.DesiredSize.Height);
                    width += child.DesiredSize.Width;
                }
            }

            return new Size(width, height);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = finalRect;
            var isVertical = Orientation == Orientation.Vertical;
            double offset = 0;

            foreach (var child in _children)
            {
                if (!child.IsVisible) continue;

                double x, y, w, h;
                if (isVertical)
                {
                    x = finalRect.X;
                    y = finalRect.Y + offset;
                    w = finalRect.Width;
                    h = child.DesiredSize.Height;
                    offset += h;
                }
                else
                {
                    x = finalRect.X + offset;
                    y = finalRect.Y;
                    w = child.DesiredSize.Width;
                    h = finalRect.Height;
                    offset += w;
                }

                child.Arrange(new Rect(x, y, w, h));
            }
        }

        public override void Render(Platform.IDrawingContext context)
        {
            // 面板本身不画,依次绘制子控件。每个子控件需要平移到自己 Bounds 的原点。
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
            // 命中测试转发给子控件
            foreach (var child in _children)
            {
                if (child.HitTest(point)) return true;
            }
            return false;
        }
    }

    /// <summary>面板接口:持有子控件集合并能增删。</summary>
    public interface IPanel
    {
        IReadOnlyList<Control> Children { get; }
        void AddChild(Control child);
        void RemoveChild(Control child);
    }

    public enum Orientation
    {
        Vertical,
        Horizontal,
    }
}
