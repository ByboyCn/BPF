using System;
using Bpf.Input;

namespace Bpf.Windows.Input
{
    /// <summary>
    /// Win32 虚拟键码(VK_*) → bpf <see cref="Key"/> 枚举转换。
    /// bpf 的 Key 枚举值有意与 VK_ 对齐(字母 A-Z=0x41-5A,数字 0-9=0x30-39,功能键等),
    /// 因此大部分情况可直接强转;少数需要映射。
    /// </summary>
    internal static class Win32KeyMap
    {
        /// <summary>把 Win32 WM_KEYDOWN/UP 的 wParam(VK code)转换为 bpf Key。</summary>
        public static Key MapKey(IntPtr wParam)
        {
            int vk = wParam.ToInt32() & 0xFF;
            return MapKey(vk);
        }

        public static Key MapKey(int vk)
        {
            // Key 枚举与 VK 在以下范围完全对齐,直接强转:
            // - 0x08-0x0D: Backspace/Tab/Enter
            // - 0x1B: Escape
            // - 0x20-0x2E: Space/PageUp..Insert/Delete
            // - 0x30-0x39: D0-D9
            // - 0x41-0x5A: A-Z
            // - 0x60-0x6F: NumPad/小键盘运算符
            // - 0x70-0x7B: F1-F12
            // - 0xA0-0xA5: 左右修饰键
            if (System.Enum.IsDefined(typeof(Key), vk))
            {
                return (Key)vk;
            }
            return Key.None;
        }

        /// <summary>读取当前修饰键状态(Ctrl/Alt/Shift/Win)。</summary>
        public static KeyModifiers GetModifiers()
        {
            var mods = KeyModifiers.None;
            if (IsKeyDown(Bpf.Windows.Interop.User32.VK_SHIFT)) mods |= KeyModifiers.Shift;
            if (IsKeyDown(Bpf.Windows.Interop.User32.VK_CONTROL)) mods |= KeyModifiers.Control;
            if (IsKeyDown(Bpf.Windows.Interop.User32.VK_MENU)) mods |= KeyModifiers.Alt;
            if (IsKeyDown(Bpf.Windows.Interop.User32.VK_LWIN) ||
                IsKeyDown(Bpf.Windows.Interop.User32.VK_RWIN)) mods |= KeyModifiers.Windows;
            return mods;
        }

        private static bool IsKeyDown(int vk)
        {
            // GetKeyState 返回 short,高位位(0x8000)表示按下
            short state = Bpf.Windows.Interop.User32.GetKeyState(vk);
            return (state & 0x8000) != 0;
        }
    }
}
