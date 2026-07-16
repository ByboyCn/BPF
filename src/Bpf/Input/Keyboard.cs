namespace Bpf.Input
{
    /// <summary>
    /// 全局键盘修饰键状态。由 Control 在 KeyDown/KeyUp 时维护,
    /// 供 PointerPressed 等无修饰键信息的场景查询(如 Shift+点击扩展选区)。
    /// 注意:这是 UI 线程上的"最近一次按键事件的修饰键状态",近似实时。
    /// </summary>
    public static class Keyboard
    {
        /// <summary>当前修饰键状态(Shift/Ctrl/Alt/Win 的按位或)。</summary>
        public static KeyModifiers Modifiers { get; internal set; }

        /// <summary>Shift 是否按下。</summary>
        public static bool IsShiftDown => (Modifiers & KeyModifiers.Shift) != 0;

        /// <summary>Ctrl 是否按下。</summary>
        public static bool IsControlDown => (Modifiers & KeyModifiers.Control) != 0;

        /// <summary>Alt 是否按下。</summary>
        public static bool IsAltDown => (Modifiers & KeyModifiers.Alt) != 0;
    }
}
