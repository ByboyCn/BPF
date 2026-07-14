using System;
using Bpf.Controls;

namespace Bpf.Layout
{
    /// <summary>
    /// 布局调度器:在每帧渲染前,对逻辑树根做 Measure/Arrange。
    /// M1 是极简实现:收到 invalidate 信号后在下一次 ExecuteLayout 时重做整树布局。
    /// </summary>
    public sealed class LayoutManager
    {
        private readonly Control _root;
        private bool _layoutQueued = true;

        public LayoutManager(Control root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
        }

        /// <summary>标记需要重做布局。</summary>
        public void Invalidate() => _layoutQueued = true;

        /// <summary>
        /// 在渲染前执行布局。若根尺寸有效则执行 Measure + Arrange。
        /// </summary>
        public void ExecuteLayout(Size availableSize)
        {
            if (!_layoutQueued)
                return;

            _layoutQueued = false;

            _root.Measure(availableSize);
            _root.Arrange(new Rect(Point.Origin, availableSize));
        }
    }
}
