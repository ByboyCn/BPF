using System;
using Bpf;
using Bpf.Platform;
using Bpf.Windows.Interop;
using SkiaSharp;

namespace Bpf.Windows.Platform
{
    /// <summary>
    /// IRenderTarget 的 SkiaSharp 实现。
    /// 用 SKSurface(CPU 光栅化)+ gdi32 SetDIBitsToDevice 呈现到 HWND。
    /// </summary>
    internal sealed class SkiaRenderTarget : IRenderTarget
    {
        private readonly IntPtr _hwnd;
        private SKBitmap? _bitmap;
        private SKCanvas? _canvas;
        private SKSurface? _surface;
        private Size _size;
        private double _scaling = 1.0;

        public SkiaRenderTarget(IntPtr hwnd)
        {
            _hwnd = hwnd;
            Recreate();
        }

        public Size PixelSize => _size;
        public double Scaling => _scaling;

        private void Recreate()
        {
            _canvas?.Dispose();
            _surface?.Dispose();
            _bitmap?.Dispose();

            User32.GetClientRect(_hwnd, out var rc);
            int w = Math.Max(1, rc.Right - rc.Left);
            int h = Math.Max(1, rc.Bottom - rc.Top);
            _size = new Size(w, h);

            var info = new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
            _bitmap = new SKBitmap(info);
            _surface = SKSurface.Create(info, _bitmap.GetPixels(), w * 4);
            _canvas = _surface?.Canvas;

            int dpi = User32.GetDpiForWindow(_hwnd);
            if (dpi > 0) _scaling = dpi / 96.0;
        }

        public void Resize(Size size)
        {
            _size = size;
            Recreate();
        }

        public IDrawingContext BeginDraw()
        {
            if (_canvas is null) throw new InvalidOperationException("SKCanvas not initialized");
            _canvas.RestoreToCount(-1);
            return new SkiaDrawingContext(_canvas);
        }

        public void Present()
        {
            if (_bitmap is null) return;
            _canvas?.Flush();
            _surface?.Flush();

            var hdc = User32.GetDC(_hwnd);
            if (hdc == IntPtr.Zero) return;
            try
            {
                var info = _bitmap.Info;
                Gdi32.SetDIBitsToDevice(hdc, _bitmap, info.Width, info.Height);
            }
            finally
            {
                User32.ReleaseDC(_hwnd, hdc);
            }
        }

        public void Dispose()
        {
            _canvas?.Dispose();
            _surface?.Dispose();
            _bitmap?.Dispose();
        }
    }
}
