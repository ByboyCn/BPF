using System;

namespace Bpf.Platform
{
    /// <summary>
    /// 平台原生位图。由 IPlatformRenderInterface.LoadBitmap 创建,供 IDrawingContext.DrawImage 绘制。
    /// </summary>
    public interface IPlatformBitmap : IDisposable
    {
        int PixelWidth { get; }
        int PixelHeight { get; }
    }
}
