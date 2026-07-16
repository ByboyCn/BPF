using System;
using Bpf.Platform;
using Bpf.Threading;
using Bpf.Windows.Platform;
using Bpf.Windows.Threading;

namespace Bpf.Windows
{
    /// <summary>
    /// IPlatformBackend 的 Windows 实现。
    /// M5:用 SkiaSharp 替代 D2D1/DWrite,不再需要 COM 初始化。
    /// </summary>
    public sealed class Win32Backend : IPlatformBackend
    {
        internal const string HandleTypeHwnd = "HWND";

        private readonly SkiaRenderInterface _renderInterface;
        private readonly Win32Dispatcher _dispatcher;

        public Win32Backend()
        {
            // 不再需要 CoInitializeEx(SkiaSharp 不依赖 COM)
            _renderInterface = new SkiaRenderInterface();
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
    /// 应用初始化扩展:在 Program 里调用 <c>Bpf.Windows.WindowsAppExtensions.UseWindows()</c>。
    /// </summary>
    public static class WindowsAppExtensions
    {
        /// <summary>用 Windows/SkiaSharp 后端初始化应用。</summary>
        public static Bpf.Application.Application UseWindows()
        {
            var backend = new Win32Backend();
            return Bpf.Application.Application.Initialize(backend);
        }
    }
}
