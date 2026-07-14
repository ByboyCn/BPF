using System;
using Bpf.Controls.Routing;

namespace Bpf.Platform
{
    /// <summary>
    /// 鼠标/触摸指针输入事件参数(可路由)。继承 RoutedEventArgs 以支持冒泡。
    /// </summary>
    public sealed class PointerEventArgs : RoutedEventArgs
    {
        public Point Position { get; internal set; }
        public PointerDeviceType DeviceType { get; }

        /// <summary>按下/松开时,被触发的按键。</summary>
        public PointerButton Button { get; }

        public PointerEventArgs(Point position, PointerDeviceType deviceType, PointerButton button)
        {
            Position = position;
            DeviceType = deviceType;
            Button = button;
        }
    }

    public enum PointerDeviceType
    {
        Mouse,
        Touch,
        Pen,
    }

    public enum PointerButton
    {
        None,
        Left,
        Middle,
        Right,
    }
}
