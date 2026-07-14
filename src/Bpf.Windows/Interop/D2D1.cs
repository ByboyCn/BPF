using System;
using System.Runtime.InteropServices;

namespace Bpf.Windows.Interop
{
    /// <summary>
    /// Direct2D 最小 COM interop。使用 vtable 槽位直接调用,完全 AOT 友好。
    /// 仅暴露 M1 所需的 ID2D1Factory / ID2D1RenderTarget / ID2D1SolidColorBrush 方法。
    /// </summary>
    /// <remarks>
    /// vtable 槽位编号说明:
    /// - 每个接口的 slot 0/1/2 是继承自 IUnknown 的 QI/AddRef/Release;
    /// - 后续槽位从该接口自身的第一个方法起算。
    /// 编号来自 d2d1.h 头文件。改动时务必比对头文件。
    /// </remarks>
    internal static unsafe class D2D1
    {
        // ── D2D1_FACTORY_TYPE ──
        public const int D2D1_FACTORY_TYPE_SINGLE_THREADED = 0;
        public const int D2D1_FACTORY_TYPE_MULTI_THREADED = 1;

        // ── D2D1_RENDER_TARGET_TYPE ──
        public const int D2D1_RENDER_TARGET_TYPE_DEFAULT = 0;
        public const int D2D1_RENDER_TARGET_TYPE_SOFTWARE = 1;
        public const int D2D1_RENDER_TARGET_TYPE_HARDWARE = 2;

        // ── D2D1_FEATURE_LEVEL ──
        public const int D2D1_FEATURE_LEVEL_DEFAULT = 0;

        // ── D2D1_RENDER_TARGET_USAGE ──
        public const int D2D1_RENDER_TARGET_USAGE_NONE = 0;

        // ── D2D1_PIXEL_FORMAT ──
        public const int D2D1_ALPHA_MODE_PREMULTIPLIED = 1;
        public const int D2D1_ALPHA_MODE_IGNORE = 3;
        public const int DXGI_FORMAT_B8G8R8A8_UNORM = 87;

        // ── D2D1_TEXT_ANTIALIAS_MODE ──
        public const int D2D1_TEXT_ANTIALIAS_MODE_DEFAULT = 0;
        public const int D2D1_TEXT_ANTIALIAS_MODE_CLEARTYPE = 1;
        public const int D2D1_TEXT_ANTIALIAS_MODE_GRAYSCALE = 2;

        // ── D2D1_DRAW_TEXT_OPTIONS ──
        public const int D2D1_DRAW_TEXT_OPTIONS_NONE = 0;
        public const int D2D1_DRAW_TEXT_OPTIONS_CLIP = 1;

        // ── 工厂函数 ──

        [DllImport("d2d1.dll", EntryPoint = "D2D1CreateFactory", CallingConvention = CallingConvention.StdCall)]
        public static extern int D2D1CreateFactory(
            int factoryType,
            ref Guid riid,
            D2D1_FACTORY_OPTIONS* pFactoryOptions,
            out IntPtr ppIFactory);

        // ── 结构 ──

        [StructLayout(LayoutKind.Sequential)]
        public struct D2D1_FACTORY_OPTIONS
        {
            public int debugLevel;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D2D1_PIXEL_FORMAT
        {
            public int format;
            public int alphaMode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D2D1_RENDER_TARGET_PROPERTIES
        {
            public int type;
            public D2D1_PIXEL_FORMAT pixelFormat;
            public float dpiX;
            public float dpiY;
            public int usage;
            public int minLevel;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D2D1_HWND_RENDER_TARGET_PROPERTIES
        {
            public IntPtr hwnd;
            public uint pixelSizeWidth;
            public uint pixelSizeHeight;
            public int presentOptions;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D2D1_RECT_F
        {
            public float left;
            public float top;
            public float right;
            public float bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D2D1_COLOR_F
        {
            public float r;
            public float g;
            public float b;
            public float a;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D2D1_POINT_2F
        {
            public float x;
            public float y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D2D1_ROUNDED_RECT
        {
            public D2D1_RECT_F rect;
            public float radiusX;
            public float radiusY;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D2D1_BRUSH_PROPERTIES
        {
            public float opacity;
            public D2D1_MATRIX_3X2_F transform;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D2D1_MATRIX_3X2_F
        {
            public float _11;
            public float _12;
            public float _21;
            public float _22;
            public float _31;
            public float _32;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D2D1_SIZE_U
        {
            public uint width;
            public uint height;
        }
    }
}
