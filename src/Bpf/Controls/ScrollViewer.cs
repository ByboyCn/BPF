using System;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 滚动查看器:内容超出可视区时,用鼠标滚轮上下滚动。
    /// 单 Child 装饰容器(参照 Border 的 Child/AttachNonPanelChildren 模式)。
    /// 渲染:PushClip(viewport) → PushTranslate(0, -offset) → child.Render → Pop → Pop。
    /// </summary>
    public sealed class ScrollViewer : Control, IContentHost
    {
        private Control? _child;
        private double _verticalOffset;
        private double _extentHeight; // 子控件完整高度
        private const double ScrollAmount = 48; // 每次滚动的像素(约 2 行)

        /// <summary>子控件(单个)。</summary>
        public Control? Child
        {
            get => _child;
            set
            {
                if (_child is not null) _child.Parent = null;
                _child = value;
                if (_child is not null)
                {
                    _child.Parent = this;
                    if (HostWindow is not null)
                        _child.AttachToHost(HostWindow, logicalRoot: LogicalRoot);
                }
                InvalidateMeasure();
            }
        }

        /// <summary>垂直滚动偏移(DIP)。</summary>
        // ── IContentHost:供命中测试遍历 ──
        Control? IContentHost.ContentChild => _child;

        public double VerticalOffset
        {
            get => _verticalOffset;
            set
            {
                var clamped = ClampOffset(value);
                if (_verticalOffset != clamped)
                {
                    _verticalOffset = clamped;
                    InvalidateArrange();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>可视区高度(Bounds.Height)。</summary>
        public double ViewportHeight => Bounds.Height;

        /// <summary>内容总高度。</summary>
        public double ExtentHeight => _extentHeight;

        /// <summary>是否可以向下滚动(还有内容在下方)。</summary>
        public bool CanScrollDown => _verticalOffset + ViewportHeight < _extentHeight;

        /// <summary>是否可以向上滚动。</summary>
        public bool CanScrollUp => _verticalOffset > 0;

        protected override void AttachNonPanelChildren(IPlatformWindow window, Control logicalRoot)
        {
            _child?.AttachToHost(window, logicalRoot);
        }

        // ── 布局 ──

        protected override Size MeasureCore(Size availableSize)
        {
            if (_child is null || !_child.IsVisible)
                return availableSize;

            // 给子控件无限高度约束,让其报告完整内容高度
            _child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            _extentHeight = _child.DesiredSize.Height;

            // ScrollViewer 自身占用 availableSize(不超出)
            double h = Math.Min(availableSize.Height, _extentHeight);
            return new Size(availableSize.Width, h);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            Bounds = finalRect;
            if (_child is null || !_child.IsVisible) return;

            // 子控件排在 (0, -VerticalOffset) 偏移处,高度 = 完整内容高度
            // 宽度减去滚动条宽度(如果需要滚动条),避免内容被滚动条遮挡
            double contentW = _extentHeight > finalRect.Height + 1
                ? finalRect.Width - ScrollBarWidth
                : finalRect.Width;
            _child.Arrange(new Rect(
                finalRect.X,
                finalRect.Y - _verticalOffset,
                contentW,
                _extentHeight));
        }

        private const double ScrollBarWidth = 14;
        private bool _isDraggingThumb;
        private double _dragStartY; // 拖拽起始时鼠标 Y
        private double _dragStartOffset; // 拖拽起始时的 offset

        // ── 渲染:Clip + Translate + ScrollBar ──

        public override void Render(IDrawingContext context)
        {
            if (_child is null || !_child.IsVisible) return;

            // 裁剪到内容区(不含滚动条宽度)
            double contentW = NeedsScrollBar ? Bounds.Width - ScrollBarWidth : Bounds.Width;
            var viewport = new Rect(0, 0, contentW, Bounds.Height);
            context.PushClip(viewport);
            try
            {
                var relX = _child.Bounds.X - Bounds.X;
                var relY = _child.Bounds.Y - Bounds.Y;
                context.PushTranslate(new Vector(relX, relY));
                try { _child.Render(context); }
                finally { context.PopTransform(); }
            }
            finally { context.PopClip(); }

            // 画滚动条(只在需要时)
            if (NeedsScrollBar)
            {
                var render = Bpf.Application.Application.Current.RenderInterface;
                double barX = Bounds.Width - ScrollBarWidth;

                // 轨道(浅灰背景)
                var track = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0))
                    .ToPlatform(render);
                try { context.FillRectangle(new Rect(barX, 0, ScrollBarWidth, Bounds.Height), track); }
                finally { track.Dispose(); }

                // 滑块(深灰)
                double thumbH = Math.Max(20, Bounds.Height * ViewportHeight / _extentHeight);
                double maxOffset = Math.Max(1, _extentHeight - ViewportHeight);
                double thumbY = (_verticalOffset / maxOffset) * (Bounds.Height - thumbH);
                var thumb = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0))
                    .ToPlatform(render);
                try { context.FillRoundedRectangle(new Rect(barX + 2, thumbY, ScrollBarWidth - 4, thumbH), 3, 3, thumb); }
                finally { thumb.Dispose(); }
            }
        }

        /// <summary>是否需要显示滚动条(内容高度 > 可视高度)。</summary>
        private bool NeedsScrollBar => _extentHeight > ViewportHeight + 1;

        // ── 滚轮滚动 ──

        protected internal override void OnMouseWheel(MouseWheelEventArgs e)
        {
            double delta = -e.Delta / 120.0 * ScrollAmount;
            VerticalOffset = _verticalOffset + delta;
            e.Handled = true;
        }

        // ── 滚动条拖拽 ──

        public override void OnPointerPressed(PointerEventArgs e)
        {
            double contentW = NeedsScrollBar ? Bounds.Width - ScrollBarWidth : Bounds.Width;
            // 点击在滚动条区域
            if (NeedsScrollBar && e.Position.X >= contentW)
            {
                double barX = Bounds.Width - ScrollBarWidth;
                double thumbH = Math.Max(20, Bounds.Height * ViewportHeight / _extentHeight);
                double maxOffset = Math.Max(1, _extentHeight - ViewportHeight);
                double thumbY = (_verticalOffset / maxOffset) * (Bounds.Height - thumbH);

                // 点击滑块:开始拖拽
                if (e.Position.Y >= thumbY && e.Position.Y <= thumbY + thumbH)
                {
                    _isDraggingThumb = true;
                    _dragStartY = e.Position.Y;
                    _dragStartOffset = _verticalOffset;
                }
                else
                {
                    // 点击轨道:跳到该位置
                    double clickRatio = e.Position.Y / Bounds.Height;
                    VerticalOffset = clickRatio * maxOffset;
                }
                e.Handled = true;
                return;
            }
            // 否则透传给子控件(不设 Handled)
        }

        public override void OnPointerMoved(PointerEventArgs e)
        {
            if (_isDraggingThumb)
            {
                double maxOffset = Math.Max(1, _extentHeight - ViewportHeight);
                double thumbH = Math.Max(20, Bounds.Height * ViewportHeight / _extentHeight);
                double trackRange = Bounds.Height - thumbH;
                if (trackRange <= 0) return;
                double dy = e.Position.Y - _dragStartY;
                VerticalOffset = _dragStartOffset + (dy / trackRange) * maxOffset;
                e.Handled = true;
            }
        }

        public override void OnPointerReleased(PointerEventArgs e)
        {
            if (_isDraggingThumb)
            {
                _isDraggingThumb = false;
                e.Handled = true;
            }
        }

        // ── 命中测试 ──

        public override bool HitTest(Point point)
        {
            if (!IsVisible || !Bounds.Contains(point)) return false;
            // point 在 ScrollViewer 的本地空间(已在窗口坐标减去祖先)
            // 子控件 Bounds 含 -offset,所以子控件的窗口坐标 = Bounds.Y - offset
            // 命中坐标需要加回 offset 转到子控件空间
            if (_child is null) return false;
            // 直接用子控件的 Bounds(已在窗口坐标系,含 offset)
            return _child.HitTest(point);
        }

        // ── 辅助 ──

        private double ClampOffset(double value)
        {
            double max = Math.Max(0, _extentHeight - ViewportHeight);
            return Math.Clamp(value, 0, max);
        }
    }
}
