using System;

namespace Bpf.Input
{
    /// <summary>键盘修饰键状态(在按键事件发生时哪些修饰键被按下)。</summary>
    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8,
    }
}
