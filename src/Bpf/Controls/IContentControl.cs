namespace Bpf.Controls
{
    /// <summary>
    /// 单子控件装饰器接口(ScrollViewer/Border 等)。
    /// 命中测试需要遍历这些控件的内部子控件(即使它们不是 IPanel)。
    /// </summary>
    internal interface IContentHost
    {
        /// <summary>内部的子控件(用于命中测试遍历)。可能为 null。</summary>
        Control? ContentChild { get; }
    }
}
