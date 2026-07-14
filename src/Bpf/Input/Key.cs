namespace Bpf.Input
{
    /// <summary>
    /// 键盘按键枚举。值尽量与 Win32 虚拟键码(VK_*)对齐,便于平台后端直接转换。
    /// 未覆盖的键用 <see cref="None"/> 表示。
    /// </summary>
    public enum Key
    {
        None = 0,

        // ── 左键/右键/中键(VK_LBUTTON 等不用在这里,键盘只关心可打印与控制键)──

        Backspace = 0x08,
        Tab = 0x09,
        Enter = 0x0D,
        Pause = 0x13,
        Capital = 0x14,    // CapsLock
        Escape = 0x1B,
        Space = 0x20,
        PageUp = 0x21,
        PageDown = 0x22,
        End = 0x23,
        Home = 0x24,

        // 方向键
        Left = 0x25,
        Up = 0x26,
        Right = 0x27,
        Down = 0x28,

        // 编辑键
        PrintScreen = 0x2C,
        Insert = 0x2D,
        Delete = 0x2E,

        // 数字键 D0-D9(VK_0x30-0x39)
        D0 = 0x30, D1, D2, D3, D4, D5, D6, D7, D8, D9,

        // 字母 A-Z(VK_0x41-0x5A)
        A = 0x41, B, C, D, E, F, G, H, I, J, K, L, M,
        N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

        // 数字小键盘(VK_NUMPAD0-9 = 0x60-0x69)
        NumPad0 = 0x60, NumPad1, NumPad2, NumPad3, NumPad4,
        NumPad5, NumPad6, NumPad7, NumPad8, NumPad9,

        // 功能键
        Multiply = 0x6A,   // 小键盘 *
        Add = 0x6B,        // 小键盘 +
        Subtract = 0x6D,   // 小键盘 -
        Decimal = 0x6E,    // 小键盘 .
        Divide = 0x6F,     // 小键盘 /

        F1 = 0x70, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

        // 修饰键
        LeftShift = 0xA0,
        RightShift = 0xA1,
        LeftCtrl = 0xA2,
        RightCtrl = 0xA3,
        LeftAlt = 0xA4,
        RightAlt = 0xA5,
    }
}
