using System;

namespace Bpf.Platform
{
    /// <summary>
    /// 渲染目标:绑定到某个窗口,负责呈现一帧图像。后端包装 D2D1 RenderTarget / DXGI swapchain 等。
    /// </summary>
    public interface IRenderTarget : IDisposable
    {
        /// <summary>渲染目标的像素尺寸(可能因 DPI 与 DIP 尺寸不同)。</summary>
        Size PixelSize { get; }

        /// <summary>缩放(DPI / 96)。控制 DIP 到像素的映射。</summary>
        double Scaling { get; }

        /// <summary>窗口尺寸或 DPI 发生变化时调用,后端重建内部缓冲。</summary>
        void Resize(Size size);

        /// <summary>
        /// 开始一帧渲染。返回 <see cref="IDrawingContext"/>,绘制完毕必须 dispose。
        /// </summary>
        IDrawingContext BeginDraw();

        /// <summary>刷新(present)到屏幕。</summary>
        void Present();
    }
}
