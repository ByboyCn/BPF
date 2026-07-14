using System;
using Bpf.Controls;
using Bpf.Platform;
using Bpf.Threading;

namespace Bpf.Application
{
    /// <summary>
    /// 应用入口。负责平台后端注入、窗口创建、生命周期管理。
    /// 用法:
    /// <code>
    /// Application.UseWindows();
    /// var window = Application.Current.CreateWindow();
    /// window.AddChild(new Button { Content = "Click me" });
    /// Application.Current.Run();
    /// </code>
    /// </summary>
    public sealed class Application
    {
        private static Application? s_current;

        public static Application Current =>
            s_current ?? throw new InvalidOperationException(
                "Application 尚未初始化。请先调用 Application.UseWindows() 或注入其他平台后端。");

        /// <summary>平台渲染接口(由后端提供)。</summary>
        public IPlatformRenderInterface RenderInterface { get; }

        /// <summary>平台后端工厂(创建窗口、dispatcher)。</summary>
        public IPlatformBackend Backend { get; }

        private Application(IPlatformBackend backend)
        {
            Backend = backend;
            RenderInterface = backend.RenderInterface;
        }

        /// <summary>
        /// 用指定后端初始化应用。一般由 Bpf.Windows 的扩展方法 UseWindows() 调用。
        /// </summary>
        public static Application Initialize(IPlatformBackend backend)
        {
            if (s_current is not null)
                throw new InvalidOperationException("Application 已初始化。");
            s_current = new Application(backend ?? throw new ArgumentNullException(nameof(backend)));
            return s_current;
        }

        /// <summary>创建一个顶级窗口。</summary>
        public Window CreateWindow(int width, int height)
        {
            var dispatcher = Backend.CreateDispatcher();
            var platformWindow = Backend.CreateWindow(width, height, RenderInterface);

            var window = new Window();
            window.Attach(platformWindow, dispatcher);

            // 把窗口的 RenderFrame 注册为 dispatcher 每帧回调
            dispatcher.SetFrameCallback(window.RenderFrame, intervalMs: 0);

            return window;
        }

        /// <summary>启动主循环。</summary>
        public void Run()
        {
            Backend.RunMainLoop();
        }

        /// <summary>请求退出。</summary>
        public void Shutdown()
        {
            Backend.Shutdown();
        }
    }
}
