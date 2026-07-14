using System;

namespace Bpf.Platform
{
    /// <summary>
    /// 平台资源句柄的统一包装。仅作为接口签名中跨平台透传的 token,
    /// 不暴露具体平台类型。后端自行解释其中的值。
    /// </summary>
    public sealed class PlatformHandle
    {
        /// <summary>原生句柄(Windows: HWND;Linux: XWindow 等)。</summary>
        public IntPtr Handle { get; }

        /// <summary>句柄所属后端标识,用于校验。</summary>
        public string HandleType { get; }

        public PlatformHandle(IntPtr handle, string handleType)
        {
            Handle = handle;
            HandleType = handleType ?? throw new ArgumentNullException(nameof(handleType));
        }
    }

    public enum FontWeight
    {
        Thin = 100,
        ExtraLight = 200,
        Light = 300,
        Normal = 400,
        Medium = 500,
        SemiBold = 600,
        Bold = 700,
        ExtraBold = 800,
        Black = 900,
    }

    public enum FontStyle
    {
        Normal,
        Italic,
        Oblique,
    }
}
