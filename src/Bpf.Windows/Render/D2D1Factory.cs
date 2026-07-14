using System;
using System.Runtime.InteropServices;
using Bpf.Windows.Interop;

namespace Bpf.Windows.Render
{
    /// <summary>
    /// 包装 ID2D1Factory COM 对象。用 vtable 调用 CreateHwndRenderTarget。
    /// </summary>
    internal sealed unsafe class D2D1Factory : IDisposable
    {
        private IntPtr _factory;

        // ID2D1Factory vtable(ID2D1Factory : IUnknown,直接继承):
        //   0 QI / 1 AddRef / 2 Release
        //   3 ReloadSystemMetrics
        //   4 GetDesktopDpi
        //   5 CreateRectangleGeometry
        //   6 CreateRoundedRectangleGeometry
        //   7 CreateEllipseGeometry
        //   8 CreateGeometryGroup
        //   9 CreateTransformedGeometry
        //   10 CreatePathGeometry
        //   11 CreateStrokeStyle
        //   12 CreateDrawingStateBlock
        //   13 CreateWicBitmapRenderTarget
        //   14 CreateHwndRenderTarget ★
        //   15 CreateDxgiSurfaceRenderTarget
        //   16 CreateDCRenderTarget
        private const int Slt_CreateHwndRenderTarget = 14;
        private const int Slt_GetDesktopDpi = 4;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateHwndRenderTargetDelegate(
            IntPtr thisPtr,
            D2D1.D2D1_RENDER_TARGET_PROPERTIES* renderTargetProperties,
            D2D1.D2D1_HWND_RENDER_TARGET_PROPERTIES* hwndRenderTargetProperties,
            out IntPtr hwndRenderTarget);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void GetDesktopDpiDelegate(IntPtr thisPtr, out float dpiX, out float dpiY);

        public D2D1Factory()
        {
            var options = new D2D1.D2D1_FACTORY_OPTIONS { debugLevel = 0 };
            var iid = Iid.ID2D1Factory;
            int hr = D2D1.D2D1CreateFactory(
                D2D1.D2D1_FACTORY_TYPE_SINGLE_THREADED,
                ref iid,
                &options,
                out _factory);
            if (hr != 0 || _factory == IntPtr.Zero)
                throw new InvalidOperationException($"D2D1CreateFactory 失败,hr=0x{hr:X8}");
        }

        public IntPtr NativePtr => _factory;

        /// <summary>
        /// 为指定 HWND 创建渲染目标(HWND RenderTarget,M1 路径,简单可靠)。
        /// </summary>
        public IntPtr CreateHwndRenderTarget(
            IntPtr hwnd,
            uint pixelWidth,
            uint pixelHeight)
        {
            var rtProps = new D2D1.D2D1_RENDER_TARGET_PROPERTIES
            {
                type = D2D1.D2D1_RENDER_TARGET_TYPE_DEFAULT,
                pixelFormat = new D2D1.D2D1_PIXEL_FORMAT
                {
                    format = D2D1.DXGI_FORMAT_B8G8R8A8_UNORM,
                    alphaMode = D2D1.D2D1_ALPHA_MODE_PREMULTIPLIED,
                },
                dpiX = 96, // 显式 96 DPI:DIP 坐标 = 像素坐标,与命中测试一致
                dpiY = 96,
                usage = D2D1.D2D1_RENDER_TARGET_USAGE_NONE,
                minLevel = D2D1.D2D1_FEATURE_LEVEL_DEFAULT,
            };

            var hwndRtProps = new D2D1.D2D1_HWND_RENDER_TARGET_PROPERTIES
            {
                hwnd = hwnd,
                pixelSizeWidth = pixelWidth,
                pixelSizeHeight = pixelHeight,
                presentOptions = 0, // D2D1_PRESENT_OPTIONS_NONE (VSync)
            };

            var create = Com.GetVTableMethod<CreateHwndRenderTargetDelegate>(
                _factory, Slt_CreateHwndRenderTarget);

            int hr = create(_factory, &rtProps, &hwndRtProps, out var rt);
            if (hr != 0)
                throw new InvalidOperationException($"CreateHwndRenderTarget 失败,hr=0x{hr:X8}");
            return rt;
        }

        public void GetDesktopDpi(out float dpiX, out float dpiY)
        {
            var getDpi = Com.GetVTableMethod<GetDesktopDpiDelegate>(_factory, Slt_GetDesktopDpi);
            getDpi(_factory, out dpiX, out dpiY);
        }

        public void Dispose()
        {
            if (_factory != IntPtr.Zero)
            {
                Com.Release(_factory);
                _factory = IntPtr.Zero;
            }
        }
    }
}
