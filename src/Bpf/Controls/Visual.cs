using System;
using Bpf.Media;
using Bpf.Platform;

namespace Bpf.Controls
{
    /// <summary>
    /// 渲染基类。所有可被绘制对象的祖先。负责持有一个 bounds(父坐标系内的矩形)
    /// 和提供 <see cref="Render"/> 钩子。
    /// </summary>
    /// <remarks>
    /// 设计:Visual 不参与布局(布局由 <see cref="Layoutable"/> 负责),
    /// 也不参与逻辑树(逻辑树由 <see cref="Control"/> 负责)。
    /// 这样分层让"只画不布局"的视觉节点(如装饰器)成为可能。
    /// </remarks>
    public abstract class Visual
    {
        private Rect _bounds;

        /// <summary>
        /// 此 Visual 在父坐标系中的边界(由 Arrange 设置)。绘制时坐标都已
        /// 相对 bounds 原点,因此 Render 内通常从 (0,0) 起笔。
        /// </summary>
        public Rect Bounds
        {
            get => _bounds;
            internal set
            {
                if (_bounds != value)
                {
                    _bounds = value;
                    OnBoundsChanged(value);
                }
            }
        }

        public double Width => Bounds.Width;
        public double Height => Bounds.Height;
        public Size Size => Bounds.Size;

        /// <summary>是否可见(不可见时不参与渲染/命中测试)。</summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 渲染入口。绘制时应从 (0,0) 起笔,坐标系是 bounds 的本地空间。
        /// 子类重写此方法完成具体绘制。
        /// </summary>
        public virtual void Render(IDrawingContext context)
        {
            // 默认无绘制。子类(如 Button)重写。
        }

        /// <summary>bounds 变化时回调(子类可重写以触发重布局/重渲染)。</summary>
        protected virtual void OnBoundsChanged(Rect newBounds) { }

        /// <summary>请求重绘。M1 由 Control 子类负责把请求冒泡到 WindowImpl。</summary>
        protected virtual void InvalidateVisual() =>
            OnInvalidateVisualRequested?.Invoke(this, EventArgs.Empty);

        internal event EventHandler? OnInvalidateVisualRequested;
    }
}
