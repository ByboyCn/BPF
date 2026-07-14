using System;

namespace Bpf.Platform
{
    /// <summary>
    /// 平台窗口实现。负责创建原生窗口、接收输入事件、提供渲染目标。
    /// 后端(Windows 下为 WindowImpl)实现本接口。
    /// </summary>
    public interface IPlatformWindow : IDisposable
    {
        /// <summary>原生窗口句柄(传给 <see cref="IPlatformRenderInterface.CreateRenderTarget"/>)。</summary>
        PlatformHandle Handle { get; }

        /// <summary>客户区尺寸(DIP)。</summary>
        Size ClientSize { get; }

        /// <summary>标题。</summary>
        string Title { get; set; }

        /// <summary>DPI 缩放。</summary>
        double Scaling { get; }

        /// <summary>是否可见。</summary>
        bool IsVisible { get; set; }

        /// <summary>显示窗口。</summary>
        void Show();

        /// <summary>隐藏窗口。</summary>
        void Hide();

        /// <summary>请求重绘(在下一次主循环迭代时触发 Render)。</summary>
        void Invalidate();

        /// <summary>为该窗口创建渲染目标(由平台渲染接口承担实际工作)。</summary>
        IRenderTarget CreateRenderTarget();

        /// <summary>
        /// 客户区尺寸变化通知。控件树 LayoutManager 据此触发根节点重新布局。
        /// </summary>
        event EventHandler<SizeChangedEventArgs>? Resized;

        /// <summary>鼠标按键按下/抬起/移动。</summary>
        event EventHandler<PointerEventArgs>? PointerPressed;
        event EventHandler<PointerEventArgs>? PointerReleased;
        event EventHandler<PointerEventArgs>? PointerMoved;

        /// <summary>窗口关闭请求。</summary>
        event EventHandler? Closed;
    }

    public sealed class SizeChangedEventArgs : EventArgs
    {
        public Size NewSize { get; }
        public SizeChangedEventArgs(Size newSize) => NewSize = newSize;
    }
}
