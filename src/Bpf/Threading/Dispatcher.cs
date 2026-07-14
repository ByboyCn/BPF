using System;
using System.Threading;

namespace Bpf.Threading
{
    /// <summary>
    /// 主线程调度器。负责跑消息循环,并在循环每帧迭代中调用已注册的渲染回调。
    /// 具体主循环(GetMessage/DispatchMessage 等)由各平台后端实现
    /// (Windows 下见 <c>Bpf.Windows.Threading.Win32Dispatcher</c>)。
    /// </summary>
    public abstract class Dispatcher
    {
        private static Dispatcher? s_current;

        /// <summary>当前线程关联的 dispatcher。由后端在初始化时设置。</summary>
        public static Dispatcher Current
        {
            get => s_current ?? throw new InvalidOperationException(
                "尚未初始化 Dispatcher。请先调用平台后端的初始化(如 Application.UseWindows())。");
            protected set => s_current = value;
        }

        /// <summary>每帧渲染前的钩子(由 Window 注册 RenderFrame)。</summary>
        private Action? _frameCallback;
        private long _frameCallbackIntervalTicks;
        private long _lastFrameTicks;

        /// <summary>设置每帧渲染回调。intervalMs 为 0 表示每帧都调用。</summary>
        public void SetFrameCallback(Action callback, int intervalMs = 0)
        {
            _frameCallback = callback;
            _frameCallbackIntervalTicks = intervalMs * TimeSpan.TicksPerMillisecond;
            _lastFrameTicks = 0;
        }

        /// <summary>后端在主循环每帧调用此方法,以触发 UI 渲染。</summary>
        protected void TickFrame()
        {
            if (_frameCallback is null) return;

            var now = DateTime.UtcNow.Ticks;
            if (_frameCallbackIntervalTicks == 0 ||
                now - _lastFrameTicks >= _frameCallbackIntervalTicks)
            {
                _frameCallback();
                _lastFrameTicks = now;
            }
        }

        /// <summary>启动主循环。由后端实现(Windows 下用 GetMessage 循环)。</summary>
        public abstract void Run();

        /// <summary>请求退出主循环。</summary>
        public abstract void Stop();
    }
}
