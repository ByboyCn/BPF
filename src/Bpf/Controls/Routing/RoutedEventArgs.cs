using System;

namespace Bpf.Controls.Routing
{
    /// <summary>
    /// 所有路由事件参数的基类。承载路由状态:事件源、Handled 标记、关联的 RoutedEvent。
    /// </summary>
    public class RoutedEventArgs : EventArgs
    {
        private bool _handled;

        /// <summary>关联的路由事件标识(在派发时由框架填入)。</summary>
        public object? RoutedEvent { get; internal set; }

        /// <summary>事件当前的处理源(冒泡时随层级变化,指当前正在处理的控件)。</summary>
        public object? Source { get; internal set; }

        /// <summary>事件的原始源(冒泡/下沉过程中保持不变,通常是命中的最深控件)。</summary>
        public object? OriginalSource { get; internal set; }

        /// <summary>是否已被处理。设为 true 后,路由的后续控件不再收到此事件。</summary>
        public bool Handled
        {
            get => _handled;
            set => _handled = value;
        }

        /// <summary>重置路由状态(用于事件对象复用)。</summary>
        internal void Reset(object? routedEvent, object? source)
        {
            RoutedEvent = routedEvent;
            Source = source;
            OriginalSource = source;
            _handled = false;
        }
    }
}
