using System;
using Bpf.Threading;
using Bpf.Windows.Interop;

namespace Bpf.Windows.Threading
{
    /// <summary>
    /// Dispatcher 的 Windows 实现:标准 GetMessage/DispatchMessage 主循环,
    /// 每帧迭代触发 RenderFrame(通过基类 TickFrame)。
    /// </summary>
    internal sealed class Win32Dispatcher : Dispatcher
    {
        private bool _running;

        public Win32Dispatcher()
        {
            Current = this;
        }

        public override void Run()
        {
            _running = true;
            User32.MSG msg;
            while (_running)
            {
                // 用 PeekMessage 非阻塞取消息,无消息时做渲染
                int ret = User32.GetMessage(out msg, IntPtr.Zero, 0, 0);
                if (ret == 0)
                {
                    // WM_QUIT
                    _running = false;
                    break;
                }
                if (ret == -1)
                {
                    // GetMessage 出错,退出
                    break;
                }

                User32.TranslateMessage(ref msg);
                User32.DispatchMessage(ref msg);

                // 每处理完一批消息,触发一次渲染
                TickFrame();
            }
        }

        public override void Stop()
        {
            _running = false;
            User32.PostQuitMessage(0);
        }
    }
}
