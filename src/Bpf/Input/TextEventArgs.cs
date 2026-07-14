using Bpf.Controls.Routing;

namespace Bpf.Input
{
    /// <summary>文本输入事件参数(对应 WM_CHAR,用于文本编辑控件的字符输入)。</summary>
    public sealed class TextEventArgs : RoutedEventArgs
    {
        /// <summary>本次输入的文本(通常是单个字符,但也可能是组合输入产生的多字符)。</summary>
        public string Text { get; }

        public TextEventArgs(string text)
        {
            Text = text ?? string.Empty;
        }
    }
}
