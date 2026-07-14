using System;

namespace Bpf.Platform
{
    /// <summary>鼠标/触摸指针输入事件参数。</summary>
    public sealed class PointerEventArgs : EventArgs
    {
        public Point Position { get; }
        public PointerDeviceType DeviceType { get; }

        /// <summary>按下/松开时,被触发的按键(M1 仅支持左键)。</summary>
        public PointerButton Button { get; }

        /// <summary>是否已被处理(用于将来事件冒泡/隧道)。</summary>
        public bool Handled { get; set; }

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
