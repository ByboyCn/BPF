using System;
using System.Runtime.InteropServices;

namespace Bpf.Windows.Interop
{
    /// <summary>
    /// D3D11 + DXGI 最小子集 P/Invoke。仅创建 device 和 swapchain 所需。
    /// </summary>
    internal static class D3D11
    {
        public const uint D3D11_CREATE_DEVICE_BGRA_SUPPORT = 0x00000020;

        public const int D3D_DRIVER_TYPE_HARDWARE = 1;
        public const int D3D_DRIVER_TYPE_WARP = 5;

        // D3D11_CREATE_DEVICE_FLAG
        public const uint D3D11_CREATE_DEVICE_DEBUG = 0x00000002;

        [DllImport("d3d11.dll")]
        public static extern int D3D11CreateDevice(
            IntPtr dxgiAdapter,
            int driverType,
            IntPtr software,
            uint flags,
            int[]? pFeatureLevels,
            int featureLevels,
            uint sdkVersion,
            out IntPtr ppDevice,
            out int pFeatureLevel,
            out IntPtr ppImmediateContext);

        // DXGI_USAGE
        public const uint DXGI_USAGE_RENDER_TARGET_OUTPUT = 0x00000020;

        // DXGI_FORMAT
        public const int DXGI_FORMAT_B8G8R8A8_UNORM = 87;

        // DXGI_SWAP_EFFECT
        public const int DXGI_SWAP_EFFECT_DISCARD = 0;
        public const int DXGI_SWAP_EFFECT_FLIP_DISCARD = 4;
    }

    /// <summary>
    /// DXGI_SWAP_CHAIN_DESC 结构。用于创建 swapchain。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct DXGI_SWAP_CHAIN_DESC
    {
        public DXGI_MODE_DESC BufferDesc;
        public DXGI_SAMPLE_DESC SampleDesc;
        public uint BufferUsage;
        public int BufferCount;
        public IntPtr OutputWindow;
        public bool Windowed;
        public int SwapEffect;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DXGI_MODE_DESC
    {
        public uint Width;
        public uint Height;
        public uint RefreshRateNumerator;
        public uint RefreshRateDenominator;
        public int Format;
        public int ScanlineOrdering;
        public int Scaling;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DXGI_SAMPLE_DESC
    {
        public uint Count;
        public uint Quality;
    }

    /// <summary>
    /// IID 常量。COM 接口标识符,用于 QueryInterface / 工厂创建。
    /// </summary>
    internal static class Iid
    {
        // IDXGIDevice
        public static readonly Guid IDXGIDevice = new Guid(
            0x54ec77fa, 0x1377, 0x44e6, 0x8c, 0x32, 0x88, 0xfd, 0x5f, 0x44, 0xc8, 0x4c);

        // IDXGIFactory1
        public static readonly Guid IDXGIFactory1 = new Guid(
            0x770aae78, 0xf26f, 0x4dba, 0xa8, 0x29, 0x25, 0x3c, 0x83, 0xd1, 0xb3, 0x87);

        // IDXGIFactory2 (用于 CreateSwapChainForHwnd,Win8+)
        public static readonly Guid IDXGIFactory2 = new Guid(
            0x50c83a1c, 0xe072, 0x4c48, 0x87, 0xb0, 0x36, 0x30, 0xfa, 0x36, 0xa6, 0xd0);

        // ID2D1Factory
        public static readonly Guid ID2D1Factory = new Guid(
            0x06152247, 0x6f50, 0x465a, 0x92, 0x45, 0x11, 0x8b, 0xfd, 0x3b, 0x60, 0x07);

        // IDWriteFactory: {b859ee5a-d838-4b5b-a2e8-1adc7d93db48}
        public static readonly Guid IDWriteFactory = new Guid(
            0xb859ee5a, 0xd838, 0x4b5b, 0xa2, 0xe8, 0x1a, 0xdc, 0x7d, 0x93, 0xdb, 0x48);
    }
}
