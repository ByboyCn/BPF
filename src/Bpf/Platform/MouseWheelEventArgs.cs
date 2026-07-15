using Bpf.Controls.Routing;

namespace Bpf.Platform
{
    /// <summary>
    /// 鼠标滚轮事件参数(可路由)。
    /// Delta 为滚轮增量(正值=向上滚,标准一格=120)。
    /// </summary>
    public sealed class MouseWheelEventArgs : RoutedEventArgs
    {
        /// <summary>滚轮位置(客户区 DIP 坐标)。</summary>
        public Point Position { get; internal set; }

        /// <summary>滚轮增量(正值=向上,标准一格=120)。</summary>
        public double Delta { get; }

        public MouseWheelEventArgs(Point position, double delta)
        {
            Position = position;
            Delta = delta;
        }
    }
}
