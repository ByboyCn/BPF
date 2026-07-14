using Bpf.Controls;
using Bpf.Controls.Routing;

namespace Bpf.Input
{
    /// <summary>焦点变化事件参数(GotFocus / LostFocus)。</summary>
    public sealed class FocusChangedEventArgs : RoutedEventArgs
    {
        /// <summary>失去焦点的控件(GotFocus 时为前一个焦点;LostFocus 时无意义,用 Source)。</summary>
        public Control? OldFocus { get; }

        /// <summary>获得焦点的控件(LostFocus 时为下一个焦点;GotFocus 时无意义,用 Source)。</summary>
        public Control? NewFocus { get; }

        public FocusChangedEventArgs(Control? oldFocus, Control? newFocus)
        {
            OldFocus = oldFocus;
            NewFocus = newFocus;
        }
    }
}
