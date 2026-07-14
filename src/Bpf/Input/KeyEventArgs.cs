using Bpf.Controls.Routing;

namespace Bpf.Input
{
    /// <summary>键盘按键事件参数(可路由)。</summary>
    public sealed class KeyEventArgs : RoutedEventArgs
    {
        /// <summary>被按下的键。</summary>
        public Key Key { get; }

        /// <summary>按下时的修饰键状态(Ctrl/Alt/Shift/Win)。</summary>
        public KeyModifiers Modifiers { get; }

        public KeyEventArgs(Key key, KeyModifiers modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }
    }
}
