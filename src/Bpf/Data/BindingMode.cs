namespace Bpf.Data
{
    /// <summary>绑定方向。</summary>
    public enum BindingMode
    {
        /// <summary>源 → 目标(数据变化时更新 UI)。默认。</summary>
        OneWay,

        /// <summary>双向:源 → 目标 且 目标 → 源。</summary>
        TwoWay,

        /// <summary>仅首次绑定,之后不更新。</summary>
        OneTime,
    }
}
