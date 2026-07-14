using System;
using Bpf.Threading;

namespace Bpf.Platform
{
    /// <summary>
    /// 平台后端总入口。聚合渲染接口、窗口/调度器工厂、主循环控制。
    /// 各平台后端实现本接口(Windows: <c>Win32PlatformBackend</c>)。
    /// </summary>
    public interface IPlatformBackend
    {
        /// <summary>渲染接口(D2D1/SK/CoreGraphics 等)。</summary>
        IPlatformRenderInterface RenderInterface { get; }

        /// <summary>创建一个原生窗口。</summary>
        IPlatformWindow CreateWindow(int width, int height, IPlatformRenderInterface render);

        /// <summary>创建主线程调度器。</summary>
        Dispatcher CreateDispatcher();

        /// <summary>启动主消息循环(阻塞直到退出)。</summary>
        void RunMainLoop();

        /// <summary>请求退出主循环。</summary>
        void Shutdown();
    }
}
