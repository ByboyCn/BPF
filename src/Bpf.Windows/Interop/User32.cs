using System;
using System.Runtime.InteropServices;

namespace Bpf.Windows.Interop
{
    internal static unsafe class User32
    {
        public const int WM_DESTROY = 0x0002;
        public const int WM_PAINT = 0x000F;
        public const int WM_SIZE = 0x0005;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_RBUTTONUP = 0x0205;
        public const int WM_DPICHANGED = 0x02E0;
        public const int WM_QUIT = 0x0012;

        // 键盘消息
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_MOUSEWHEEL = 0x020A;
        public const int WM_SYSKEYUP = 0x0105;

        // 修饰键虚拟键码(用于 GetKeyState 判断修饰键状态)
        public const int VK_SHIFT = 0x10;
        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12;       // Alt
        public const int VK_LWIN = 0x5B;
        public const int VK_RWIN = 0x5C;

        public const int CS_HREDRAW = 0x0002;
        public const int CS_VREDRAW = 0x0001;

        public const uint SW_SHOW = 5;

        public const int CW_USEDEFAULT = unchecked((int)0x80000000);

        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WNDCLASSEX
        {
            public int cbSize;
            public uint style;
            public IntPtr lpfnWndProc; // WndProc 函数指针
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public IntPtr lpszMenuName;   // NULL = 无菜单
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hWnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
            public int reserved0;
            public int reserved1;
            public int reserved2;
            public int reserved3;
            public int reserved4;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "RegisterClassExW")]
        public static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateWindowExW")]
        public static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
            uint dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        // char* 版本,绕过 NativeAOT 下 string marshal 的截断问题。
        [DllImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true)]
        public static extern unsafe IntPtr CreateWindowExPtr(
            uint dwExStyle,
            char* lpClassName,
            char* lpWindowName,
            uint dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SetWindowTextW")]
        public static extern bool SetWindowText(IntPtr hWnd,
            [MarshalAs(UnmanagedType.LPWStr)] string lpString);

        // 接受原生 char* 的版本,绕过 NativeAOT 下不可靠的 string marshal。
        // string 在 .NET 内部就是 UTF-16,fixed char* 取地址直接传给 W 函数即可。
        [DllImport("user32.dll", EntryPoint = "SetWindowTextW", SetLastError = true)]
        public static extern unsafe bool SetWindowTextPtr(IntPtr hWnd, char* lpString);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextLengthW")]
        public static extern int GetWindowTextLengthPtr(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextW")]
        public static extern unsafe int GetWindowTextPtr(IntPtr hWnd, char* lpString, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
        public static extern IntPtr DefWindowProcPtr(IntPtr hWnd, uint Msg, IntPtr wParam, char* lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetWindowTextLengthW")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetWindowTextW")]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32.dll")]
        public static extern IntPtr BeginPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool AdjustWindowRectEx(ref RECT lpRect, uint dwStyle, bool bMenu, uint dwExStyle);

        [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        // ── 消息循环 ──

        [DllImport("user32.dll")]
        public static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        public static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);

        // 标准 cursor ID
        public static readonly IntPtr IDC_ARROW = new IntPtr(32512);

        // ── DPI ──

        [DllImport("user32.dll")]
        public static extern int GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetricsForDpi(int nIndex, uint dpi);

        // 鼠标滚轮:WM_MOUSEWHEEL 的 lParam 是屏幕坐标,需转客户区坐标
        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;

        // 窗口样式
        public const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
        public const uint WS_VISIBLE = 0x10000000;
        public const uint WS_CLIPCHILDREN = 0x02000000;
        public const uint WS_CLIPSIBLINGS = 0x04000000;
    }

    internal static class Kernel32
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();
    }

    /// <summary>OLE/COM 初始化。所有 COM 图形 API(D2D1/DWrite)都要求
    /// 主线程已调用 CoInitializeEx。</summary>
    internal static class Ole32
    {
        // COINIT_APARTMENTTHREADED = 0x2; COINIT_MULTITHREADED = 0x0
        public const uint COINIT_APARTMENTTHREADED = 0x2;

        // RPC_E_CHANGED_MODE = 0x80010106 (已用不同模式初始化,可忽略)
        public const int RPC_E_CHANGED_MODE = unchecked((int)0x80010106);

        // S_FALSE = 1 (已初始化,正常)
        public const int S_FALSE = 1;

        [DllImport("ole32.dll")]
        public static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

        [DllImport("ole32.dll")]
        public static extern void CoUninitialize();
    }
}
