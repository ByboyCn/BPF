using System;
using Bpf.Media;
using Bpf.Platform;
using Bpf.Windows.Render;

namespace Bpf.Windows.Platform
{
    /// <summary>
    /// IPlatformRenderInterface 的 Windows/D2D1 实现。
    /// 持有 D2D1 工厂和 DWrite 工厂(单例),创建画笔、文本格式、渲染目标。
    /// </summary>
    internal sealed class Win32RenderInterface : IPlatformRenderInterface
    {
        private readonly D2D1Factory _d2dFactory;
        private readonly DWriteFactory _dwriteFactory;

        public Win32RenderInterface(D2D1Factory d2dFactory, DWriteFactory dwriteFactory)
        {
            _d2dFactory = d2dFactory;
            _dwriteFactory = dwriteFactory;
        }

        public D2D1Factory D2DFactory => _d2dFactory;
        public DWriteFactory DWriteFactory => _dwriteFactory;

        public IPlatformBrush CreateSolidColorBrush(Color color)
        {
            // 注意:SolidColorBrush 需要绑定到某个 RenderTarget 的 CreateSolidColorBrush。
            // 这里无法独立创建(brush 必须在某个 RenderTarget 上下文里)。
            // 解决:用 lazy 绑定 —— 把 (Color) 暂存,在 RenderTarget 首次使用时物化。
            // 见 Win32RenderTarget.MaterializeBrush。
            return new Win32DeferredBrush(color);
        }

        public IRenderTarget CreateRenderTarget(PlatformHandle windowHandle)
        {
            if (windowHandle.HandleType != Win32Backend.HandleTypeHwnd)
                throw new ArgumentException(
                    $"期望 HWND 句柄,实际:{windowHandle.HandleType}", nameof(windowHandle));

            return new Win32RenderTarget(_d2dFactory, _dwriteFactory, windowHandle.Handle);
        }

        public IPlatformTextFormat CreateTextFormat(
            string fontFamily, double fontSize,
            FontWeight fontWeight = FontWeight.Normal,
            FontStyle fontStyle = FontStyle.Normal)
        {
            int w = fontWeight switch
            {
                FontWeight.Bold => DWriteInterop.DWRITE_FONT_WEIGHT_BOLD,
                FontWeight.Normal => DWriteInterop.DWRITE_FONT_WEIGHT_NORMAL,
                _ => (int)fontWeight, // FontWeight 枚举值本身即 100~900
            };
            int s = fontStyle switch
            {
                FontStyle.Italic => DWriteInterop.DWRITE_FONT_STYLE_ITALIC,
                FontStyle.Oblique => DWriteInterop.DWRITE_FONT_STYLE_OBLIQUE,
                _ => DWriteInterop.DWRITE_FONT_STYLE_NORMAL,
            };

            var native = _dwriteFactory.CreateTextFormat(
                fontFamily, w, s, (float)fontSize);
            return new Win32TextFormat(_dwriteFactory, native, fontFamily, (float)fontSize);
        }
    }

    /// <summary>DWrite 字重/字风格常量映射(避免在 Interop 文件里散落)。</summary>
    internal static class DWriteInterop
    {
        public const int DWRITE_FONT_WEIGHT_NORMAL = 400;
        public const int DWRITE_FONT_WEIGHT_BOLD = 700;
        public const int DWRITE_FONT_STYLE_NORMAL = 0;
        public const int DWRITE_FONT_STYLE_ITALIC = 1;
        public const int DWRITE_FONT_STYLE_OBLIQUE = 2;
    }
}
