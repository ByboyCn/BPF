using System;
using System.Runtime.InteropServices;
using Bpf;
using Bpf.Input;
using Bpf.Platform;
using Bpf.Windows.Input;
using Bpf.Windows.Interop;

namespace Bpf.Windows.Platform
{
    /// <summary>
    /// IPlatformWindow 的 Windows 实现。注册窗口类、创建 HWND、WndProc 派发。
    /// </summary>
    internal sealed unsafe class WindowImpl : IPlatformWindow
    {
        private static uint s_classAtom;
        private static User32.WndProc? s_wndProc; // 防止 GC 回收

        private readonly IntPtr _hinstance;
        private readonly IntPtr _hwnd;
        private readonly IPlatformRenderInterface _render;
        private string _title;
        private bool _visible;
        private bool _disposed;

        public event EventHandler<SizeChangedEventArgs>? Resized;
        public event EventHandler<PointerEventArgs>? PointerPressed;
        public event EventHandler<PointerEventArgs>? PointerReleased;
        public event EventHandler<PointerEventArgs>? PointerMoved;
        public event EventHandler<KeyEventArgs>? KeyDown;
        public event EventHandler<KeyEventArgs>? KeyUp;
        public event EventHandler<TextEventArgs>? TextInput;
        public event EventHandler<MouseWheelEventArgs>? MouseWheel;
        public event EventHandler? Closed;

        public WindowImpl(int width, int height, IPlatformRenderInterface render)
        {
            _render = render;
            _hinstance = Kernel32.GetModuleHandle(null);
            _title = "bpf";

            EnsureClassRegistered(_hinstance);

            // 计算包含非客户区的窗口尺寸
            var rc = new User32.RECT { Left = 0, Top = 0, Right = width, Bottom = height };
            User32.AdjustWindowRectEx(ref rc, User32.WS_OVERLAPPEDWINDOW, false, 0);
            int winW = rc.Right - rc.Left;
            int winH = rc.Bottom - rc.Top;

            // 用 char* 版本绕过 string marshal(NativeAOT 下会截断字符串)
            fixed (char* pClass = ClassName)
            fixed (char* pTitle = _title)
            {
                _hwnd = User32.CreateWindowExPtr(
                    0,
                    pClass,
                    pTitle,
                    User32.WS_OVERLAPPEDWINDOW,
                    User32.CW_USEDEFAULT, User32.CW_USEDEFAULT,
                    winW, winH,
                    IntPtr.Zero, IntPtr.Zero, _hinstance, IntPtr.Zero);
            }

            if (_hwnd == IntPtr.Zero)
                throw new InvalidOperationException(
                    $"CreateWindow 失败,lastError={Kernel32.GetLastError()}");

            // 把 this 指针塞进 HWND 的用户数据(GWLP_USERDATA),WndProc 里取回
            SetWindowLongPtr(_hwnd, -21 /* GWLP_USERDATA */, GCHandle.ToIntPtr(AllocSelf()));
        }


        private GCHandle _selfHandle;
        private GCHandle AllocSelf()
        {
            _selfHandle = GCHandle.Alloc(this);
            return _selfHandle;
        }

        public PlatformHandle Handle => new PlatformHandle(_hwnd, Win32Backend.HandleTypeHwnd);

        public Size ClientSize
        {
            get
            {
                User32.GetClientRect(_hwnd, out var rc);
                double dpiScale = Scaling;
                return new Size((rc.Right - rc.Left) / dpiScale, (rc.Bottom - rc.Top) / dpiScale);
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value ?? "";
                if (_hwnd != IntPtr.Zero)
                {
                    // 绕过 P/Invoke 字符串 marshal(在 NativeAOT 下不可靠):
                    // 手工把字符串编码成 UTF-16 并固定到 native 内存,直接调 SetWindowTextW。
                    SetWindowTextManual(_hwnd, _title);
                }
            }
        }

        /// <summary>手工 marshal:绕过 P/Invoke string marshaler(在 NativeAOT 下不可靠)。
        /// .NET string 内部布局是 [length][UTF-16 chars]['\0'],fixed char* 直接得到
        /// null 终止的 UTF-16 指针,与 SetWindowTextW 的 LPCWSTR 完全匹配。</summary>
        private static unsafe void SetWindowTextManual(IntPtr hwnd, string text)
        {
            fixed (char* p = text)
            {
                User32.SetWindowTextPtr(hwnd, p);
            }
        }

        public double Scaling
        {
            get
            {
                int dpi = User32.GetDpiForWindow(_hwnd);
                return dpi > 0 ? dpi / 96.0 : 1.0;
            }
        }

        public bool IsVisible
        {
            get => _visible;
            set
            {
                if (value) Show();
                else Hide();
            }
        }

        public void Show()
        {
            User32.ShowWindow(_hwnd, User32.SW_SHOW);
            User32.UpdateWindow(_hwnd);
            _visible = true;
        }

        public void Hide()
        {
            User32.ShowWindow(_hwnd, 0);
            _visible = false;
        }

        public void Invalidate()
        {
            User32.InvalidateRect(_hwnd, IntPtr.Zero, false);
        }

        public IRenderTarget CreateRenderTarget() => _render.CreateRenderTarget(Handle);

        // ── 窗口类注册 ──

        private const string ClassName = "bpf.WindowClass";

        private static void EnsureClassRegistered(IntPtr hinstance)
        {
            if (s_classAtom != 0) return;

            s_wndProc = new User32.WndProc(StaticWndProc);

            var wc = new User32.WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<User32.WNDCLASSEX>(),
                style = User32.CS_HREDRAW | User32.CS_VREDRAW,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_wndProc),
                hInstance = hinstance,
                hCursor = User32.LoadCursor(IntPtr.Zero, User32.IDC_ARROW),
                hbrBackground = IntPtr.Zero,
                lpszClassName = ClassName,
            };

            s_classAtom = User32.RegisterClassEx(ref wc);
            if (s_classAtom == 0)
                throw new InvalidOperationException(
                    $"RegisterClassEx 失败,lastError={Kernel32.GetLastError()}");
        }

        // ── WndProc ──

        private static IntPtr StaticWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            // 从 GWLP_USERDATA 取回 this
            var selfPtr = GetWindowLongPtr(hWnd, -21);
            if (selfPtr != IntPtr.Zero)
            {
                var target = (WindowImpl?)GCHandle.FromIntPtr(selfPtr).Target;
                if (target is not null)
                {
                    return target.WndProc(hWnd, msg, wParam, lParam);
                }
            }
            return User32.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case User32.WM_SIZE:
                    {
                        int newWidth = lParam.ToInt32() & 0xFFFF;
                        int newHeight = (lParam.ToInt32() >> 16) & 0xFFFF;
                        double s = Scaling;
                        var newSize = new Size(newWidth / s, newHeight / s);
                        Resized?.Invoke(this, new SizeChangedEventArgs(newSize));
                        return IntPtr.Zero;
                    }

                case User32.WM_LBUTTONDOWN:
                case User32.WM_RBUTTONDOWN:
                    {
                        var p = PointFromLParam(lParam);
                        var btn = msg == User32.WM_LBUTTONDOWN ? PointerButton.Left : PointerButton.Right;
                        PointerPressed?.Invoke(this, new PointerEventArgs(p, PointerDeviceType.Mouse, btn));
                        return IntPtr.Zero;
                    }

                case User32.WM_LBUTTONUP:
                case User32.WM_RBUTTONUP:
                    {
                        var p = PointFromLParam(lParam);
                        var btn = msg == User32.WM_LBUTTONUP ? PointerButton.Left : PointerButton.Right;
                        PointerReleased?.Invoke(this, new PointerEventArgs(p, PointerDeviceType.Mouse, btn));
                        return IntPtr.Zero;
                    }

                case User32.WM_MOUSEMOVE:
                    {
                        var p = PointFromLParam(lParam);
                        PointerMoved?.Invoke(this, new PointerEventArgs(p, PointerDeviceType.Mouse, PointerButton.None));
                        return IntPtr.Zero;
                    }

                case User32.WM_KEYDOWN:
                case User32.WM_SYSKEYDOWN:
                    {
                        var key = Win32KeyMap.MapKey(wParam);
                        var mods = Win32KeyMap.GetModifiers();
                        KeyDown?.Invoke(this, new KeyEventArgs(key, mods));
                        return IntPtr.Zero;
                    }

                case User32.WM_KEYUP:
                case User32.WM_SYSKEYUP:
                    {
                        var key = Win32KeyMap.MapKey(wParam);
                        var mods = Win32KeyMap.GetModifiers();
                        KeyUp?.Invoke(this, new KeyEventArgs(key, mods));
                        return IntPtr.Zero;
                    }

                case User32.WM_CHAR:
                    {
                        // wParam 是 UTF-16 码元
                        int ch = wParam.ToInt32() & 0xFFFF;
                        if (ch >= 32 || ch == '\r' || ch == '\t' || ch == '\b')
                        {
                            TextInput?.Invoke(this, new TextEventArgs(((char)ch).ToString()));
                        }
                        return IntPtr.Zero;
                    }

                case User32.WM_MOUSEWHEEL:
                    {
                        // wParam 高字是有符号 delta(标准一格=120),低字是按键状态
                        // 用 ToInt64 避免 checked 上下文下的 OverflowException
                        long wParam64 = wParam.ToInt64();
                        short delta = (short)((wParam64 >> 16) & 0xFFFF);
                        // lParam 是屏幕坐标,需转客户区坐标
                        long lParam64 = lParam.ToInt64();
                        var screenPoint = new User32.POINT
                        {
                            X = (int)(short)(lParam64 & 0xFFFF),
                            Y = (int)(short)((lParam64 >> 16) & 0xFFFF),
                        };
                        User32.ScreenToClient(_hwnd, ref screenPoint);
                        double s = Scaling;
                        var pos = new Point(screenPoint.X / s, screenPoint.Y / s);
                        MouseWheel?.Invoke(this, new MouseWheelEventArgs(pos, delta));
                        return IntPtr.Zero;
                    }

                case User32.WM_DESTROY:
                    Closed?.Invoke(this, EventArgs.Empty);
                    User32.PostQuitMessage(0);
                    return IntPtr.Zero;
            }

            return User32.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private Point PointFromLParam(IntPtr lParam)
        {
            int v = lParam.ToInt32();
            short x = (short)(v & 0xFFFF);
            short y = (short)((v >> 16) & 0xFFFF);
            double s = Scaling;
            return new Point(x / s, y / s);
        }

        // ── GetWindowLongPtr 兼容(x86/x64) ──

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            return new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        private static void SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value)
        {
            if (IntPtr.Size == 8)
                SetWindowLongPtr64(hWnd, nIndex, value);
            else
                SetWindowLong32(hWnd, nIndex, value.ToInt32());
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr value);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int value);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_hwnd != IntPtr.Zero)
            {
                User32.DestroyWindow(_hwnd);
            }
            if (_selfHandle.IsAllocated) _selfHandle.Free();
        }
    }
}
