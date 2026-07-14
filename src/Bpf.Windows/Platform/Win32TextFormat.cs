using System;
using Bpf;
using Bpf.Media;
using Bpf.Platform;
using Bpf.Windows.Interop;
using Bpf.Windows.Render;

namespace Bpf.Windows.Platform
{
    /// <summary>
    /// IPlatformTextFormat 的 Windows/DWrite 实现。同时承担文本度量。
    /// </summary>
    internal sealed class Win32TextFormat : IPlatformTextFormat
    {
        private readonly DWriteFactory _dwrite;
        private IntPtr _native;
        public IntPtr NativePtr => _native;

        public string FontFamily { get; }
        public double FontSize { get; }
        public FontWeight FontWeight { get; }
        public FontStyle FontStyle { get; }

        public Win32TextFormat(DWriteFactory dwrite, IntPtr native,
            string fontFamily, float fontSize)
        {
            _dwrite = dwrite;
            _native = native;
            FontFamily = fontFamily;
            FontSize = fontSize;
        }

        public Size MeasureText(string text) =>
            DWriteTextFormat.Measure(_dwrite, _native, text);

        public void Dispose()
        {
            if (_native != IntPtr.Zero)
            {
                Com.Release(_native);
                _native = IntPtr.Zero;
            }
        }
    }

    /// <summary>
    /// 延迟物化画笔:持有 Color,首次在某个 RenderTarget 上使用时被物化为 native brush。
    /// </summary>
    internal sealed class Win32DeferredBrush : IPlatformBrush
    {
        public Color Color { get; }
        // 物化后的 native brush 句柄(由所属 RenderTarget 释放,这里只缓存)
        private IntPtr _materializedPtr;

        public Win32DeferredBrush(Color color) => Color = color;

        /// <summary>
        /// 在指定 D2D1RenderTarget 上获取(必要时创建)物化的 brush。
        /// </summary>
        public IntPtr Materialize(Bpf.Windows.Render.D2D1RenderTarget rt)
        {
            if (_materializedPtr != IntPtr.Zero)
                return _materializedPtr;

            var col = new Interop.D2D1.D2D1_COLOR_F
            {
                r = Color.R / 255f,
                g = Color.G / 255f,
                b = Color.B / 255f,
                a = (Color.A / 255f) * 1.0f,
            };
            _materializedPtr = rt.CreateSolidColorBrush(col);
            return _materializedPtr;
        }

        /// <summary>RenderTarget 销毁时通知本 brush 失效。</summary>
        public void Invalidate() => _materializedPtr = IntPtr.Zero;

        public void Dispose()
        {
            // native brush 由所属 RenderTarget 释放
            _materializedPtr = IntPtr.Zero;
        }
    }
}
