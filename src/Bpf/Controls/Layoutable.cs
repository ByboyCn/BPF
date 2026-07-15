using System;
using Bpf.Utilities;

namespace Bpf.Controls
{
    /// <summary>
    /// 布局参与基类。实现经典的 Measure/Arrange 双阶段布局协议
    /// (与 WPF/Avalonia 一致)。
    /// </summary>
    /// <remarks>
    /// 布局协议:
    /// 1. <see cref="Measure"/>:自下而上,子控件告诉父"在给定约束下我希望多大"。
    /// 2. <see cref="Arrange">:自上而下,父决定子最终位置/大小。
    /// 任何布局或尺寸相关属性变更,都应调用 MeasureInvalidated/ArrangeInvalidated
    /// 让 <see cref="LayoutManager"/> 在下一帧重做布局。
    /// </remarks>
    public abstract class Layoutable : Visual
    {
        private Size _desiredSize;
        private Size? _measureConstraint;
        private bool _measureInvalidated = true;
        private bool _arrangeInvalidated = true;

        /// <summary>
        /// 测量后的期望尺寸。父布局用此值决定子位置。仅在 <see cref="Measure"/> 后有效。
        /// </summary>
        public Size DesiredSize => _desiredSize;

        /// <summary>是否需要重新测量。</summary>
        public bool IsMeasureValid => !_measureInvalidated;

        /// <summary>是否需要重新排列。</summary>
        public bool IsArrangeValid => !_arrangeInvalidated;

        /// <summary>
        /// 测量阶段。约束由父给出。返回的尺寸会缓存在 <see cref="DesiredSize"/>。
        /// 子类重写 <see cref="MeasureCore"/> 提供具体测量逻辑,不要重写本方法。
        /// </summary>
        public void Measure(Size availableSize)
        {
            // 若约束未变且未失效,直接复用上次结果
            if (!_measureInvalidated &&
                _measureConstraint.HasValue &&
                _measureConstraint.Value == availableSize)
            {
                return;
            }

            _measureConstraint = availableSize;
            var measured = MeasureCore(availableSize);
            _desiredSize = measured;
            _measureInvalidated = false;
            // 测量变化必然导致需重新排列
            InvalidateArrange();
        }

        /// <summary>
        /// 排列阶段。父给定最终矩形。
        /// 子类重写 <see cref="ArrangeCore"/> 提供具体排列逻辑,不要重写本方法。
        /// </summary>
        public void Arrange(Rect finalRect)
        {
            if (_arrangeInvalidated || Bounds != finalRect)
            {
                ArrangeCore(finalRect);
                _arrangeInvalidated = false;
            }
        }

        /// <summary>子类实现具体测量逻辑。返回的尺寸不应大于 availableSize。</summary>
        protected abstract Size MeasureCore(Size availableSize);

        /// <summary>子类实现具体排列逻辑(设置 Bounds 及排列子控件)。</summary>
        protected abstract void ArrangeCore(Rect finalRect);

        /// <summary>使测量结果失效。</summary>
        public void InvalidateMeasure()
        {
            if (!_measureInvalidated)
            {
                _measureInvalidated = true;
                // 排列也失效(测量变了必须重排)
                _arrangeInvalidated = true;
                OnInvalidateMeasureRequested?.Invoke(this, EventArgs.Empty);
            }
            // 关键:向上传播,让父控件也失效(否则父.Measure 缓存命中会跳过对本控件的重测)
            if (this is Control c && c.Parent is Control parent)
            {
                parent.InvalidateMeasure();
            }
        }

        /// <summary>使排列结果失效(测量也会随之失效)。</summary>
        public void InvalidateArrange()
        {
            if (!_arrangeInvalidated)
            {
                _arrangeInvalidated = true;
                OnInvalidateArrangeRequested?.Invoke(this, EventArgs.Empty);
                // 排列失效通常也意味着视觉失效
                InvalidateVisual();
            }
        }

        internal event EventHandler? OnInvalidateMeasureRequested;
        internal event EventHandler? OnInvalidateArrangeRequested;
    }
}
