using System;
using System.Collections.Generic;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>停靠方向。</summary>
    public enum Dock
    {
        Left,
        Top,
        Right,
        Bottom,
    }

    /// <summary>
    /// 停靠面板。子控件按附加属性 DockPanel.Dock 停靠到剩余空间的某一边。
    /// LastChildFill=true(默认)时,最后一个子控件填满剩余空间。
    /// </summary>
    public sealed class DockPanel : Control, IPanel
    {
        private readonly List<Control> _children = new List<Control>();

        // ── 附加属性 ──

        public static readonly AttachedProperty<Dock> DockProperty =
            AttachedProperty<Dock>.Register<DockPanel>("Dock", Dock.Left, affectsArrange: true);

        public static Dock GetDock(Control e) => e.GetValue(DockProperty);
        public static void SetDock(Control e, Dock value) => e.SetValue(DockProperty, value);

        // ── 属性 ──

        /// <summary>最后一个子控件是否填满剩余空间(默认 true)。</summary>
        public bool LastChildFill { get; set; } = true;

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

        // ── 布局 ──

        protected override Size MeasureCore(Size availableSize)
        {
            double parentWidth = 0;
            double parentHeight = 0;
            double accumulatedWidth = 0;
            double accumulatedHeight = 0;

            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                if (!child.IsVisible) continue;

                var childAvailable = new Size(
                    Math.Max(0, availableSize.Width - accumulatedWidth),
                    Math.Max(0, availableSize.Height - accumulatedHeight));

                child.Measure(childAvailable);
                var desired = child.DesiredSize;

                bool isLast = LastChildFill && i == _children.Count - 1;
                var dock = GetDock(child);

                switch (dock)
                {
                    case Dock.Left:
                    case Dock.Right:
                        parentHeight = Math.Max(parentHeight, accumulatedHeight + desired.Height);
                        if (dock == Dock.Left)
                            accumulatedWidth += desired.Width;
                        else
                            parentWidth = Math.Max(parentWidth, accumulatedWidth + desired.Width);
                        break;
                    case Dock.Top:
                    case Dock.Bottom:
                        parentWidth = Math.Max(parentWidth, accumulatedWidth + desired.Width);
                        if (dock == Dock.Top)
                            accumulatedHeight += desired.Height;
                        else
                            parentHeight = Math.Max(parentHeight, accumulatedHeight + desired.Height);
                        break;
                }
            }

            return new Size(parentWidth, parentHeight);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = finalRect;
            double x = finalRect.X;
            double y = finalRect.Y;
            double width = finalRect.Width;
            double height = finalRect.Height;

            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                if (!child.IsVisible) continue;

                bool isLast = LastChildFill && i == _children.Count - 1;
                var dock = GetDock(child);
                var desired = child.DesiredSize;

                Rect childRect;
                switch (dock)
                {
                    case Dock.Left:
                        childRect = new Rect(x, y, desired.Width, height);
                        x += desired.Width;
                        width -= desired.Width;
                        break;
                    case Dock.Right:
                        childRect = new Rect(x + width - desired.Width, y, desired.Width, height);
                        width -= desired.Width;
                        break;
                    case Dock.Top:
                        childRect = new Rect(x, y, width, desired.Height);
                        y += desired.Height;
                        height -= desired.Height;
                        break;
                    case Dock.Bottom:
                        childRect = new Rect(x, y + height - desired.Height, width, desired.Height);
                        height -= desired.Height;
                        break;
                    default:
                        childRect = new Rect(x, y, width, height);
                        break;
                }

                // LastChildFill:最后一个填满剩余
                if (isLast)
                    child.Arrange(new Rect(x, y, width, height));
                else
                    child.Arrange(childRect);
            }
        }

        // ── 渲染 ──

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
    }
}
