using System;
using Bpf.Media;
using Bpf.Platform;

namespace Bpf.Windows.Platform
{
    /// <summary>
    /// IPlatformRenderInterface 的 SkiaSharp 实现。
    /// 替代 D2D1/DWrite/WIC,纯 SkiaSharp(P/Invoke 到 libSkiaSharp,不依赖 COM)。
    /// </summary>
    internal sealed class SkiaRenderInterface : IPlatformRenderInterface
    {
        public IPlatformBrush CreateSolidColorBrush(Color color)
        {
            return new SkiaSolidBrush(color);
        }

        public IRenderTarget CreateRenderTarget(PlatformHandle windowHandle)
        {
            if (windowHandle.HandleType != Win32Backend.HandleTypeHwnd)
                throw new ArgumentException(
                    $"期望 HWND 句柄,实际:{windowHandle.HandleType}", nameof(windowHandle));
            return new SkiaRenderTarget(windowHandle.Handle);
        }

        public IPlatformTextFormat CreateTextFormat(
            string fontFamily, double fontSize,
            FontWeight fontWeight = FontWeight.Normal,
            FontStyle fontStyle = FontStyle.Normal)
        {
            return new SkiaTextFormat(fontFamily, fontSize, fontWeight, fontStyle);
        }

        public IPlatformBitmap LoadBitmap(string path)
        {
            return new SkiaBitmap(path);
        }
    }
}
