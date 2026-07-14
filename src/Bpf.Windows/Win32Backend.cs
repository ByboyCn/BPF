using System;
using Bpf.Platform;
using Bpf.Threading;
using Bpf.Windows.Platform;
using Bpf.Windows.Render;
using Bpf.Windows.Threading;

namespace Bpf.Windows
{
    /// <summary>
    /// IPlatformBackend 的 Windows 实现。聚合 D2D1/DWrite 工厂、窗口工厂、主循环。
    /// </summary>
    public sealed class Win32Backend : IPlatformBackend
    {
        internal const string HandleTypeHwnd = "HWND";

        private readonly Win32RenderInterface _renderInterface;
        private readonly Win32Dispatcher _dispatcher;

        public Win32Backend()
        {
            // 初始化 COM(单线程公寓)。D2D1/DWrite 必需。
            // 忽略 RPC_E_CHANGED_MODE(已用其他模式初始化)和 S_FALSE(已初始化)。
            int hr = Bpf.Windows.Interop.Ole32.CoInitializeEx(
                IntPtr.Zero, Bpf.Windows.Interop.Ole32.COINIT_APARTMENTTHREADED);
            if (hr < 0 && hr != Bpf.Windows.Interop.Ole32.RPC_E_CHANGED_MODE)
            {
                throw new InvalidOperationException($"CoInitializeEx 失败,hr=0x{hr:X8}");
            }

            var d2d = new D2D1Factory();
            var dwrite = new DWriteFactory();
            _renderInterface = new Win32RenderInterface(d2d, dwrite);
            _dispatcher = new Win32Dispatcher();
        }

        public IPlatformRenderInterface RenderInterface => _renderInterface;

        public IPlatformWindow CreateWindow(int width, int height, IPlatformRenderInterface render)
        {
            return new WindowImpl(width, height, _renderInterface);
        }

        public Dispatcher CreateDispatcher() => _dispatcher;

        public void RunMainLoop()
        {
            _dispatcher.Run();
        }

        public void Shutdown()
        {
            _dispatcher.Stop();
        }
    }

    /// <summary>
    /// 应用初始化扩展:在 Program 里调用 <c>Bpf.Application.Application.UseWindows()</c>。
    /// </summary>
    public static class WindowsAppExtensions
    {
        /// <summary>用 Windows/D2D1 后端初始化应用。</summary>
        public static Bpf.Application.Application UseWindows()
        {
            var backend = new Win32Backend();
            return Bpf.Application.Application.Initialize(backend);
        }
    }
}
