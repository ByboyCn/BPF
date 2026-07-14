using Bpf.Media;

namespace Bpf.Platform
{
    /// <summary>
    /// 平台渲染能力抽象。由各平台后端(Bpf.Windows 等)实现,通过
    /// <see cref="Application.Application.UsePlatform(IPlatformRenderInterface)"/> 注入。
    /// </summary>
    /// <remarks>
    /// 设计要点:
    /// - 这是核心库与原生图形栈之间的唯一接缝;Windows 下指向 D2D1/DWrite,
    ///   Linux 下将来可指向 cairo/SK,macOS 下指向 CoreGraphics。
    /// - 接口刻意保持极小集;新增能力时优先扩接口,避免泄漏平台类型。
    /// </remarks>
    public interface IPlatformRenderInterface
    {
        /// <summary>创建纯色画笔的平台句柄。</summary>
        IPlatformBrush CreateSolidColorBrush(Color color);

        /// <summary>
        /// 为指定窗口句柄创建渲染目标。窗口句柄的具体内容由平台定义
        /// (Windows 下是 HWND;Linux 下将来是 XWindow 等)。
        /// </summary>
        IRenderTarget CreateRenderTarget(PlatformHandle windowHandle);

        /// <summary>
        /// 创建文本格式(字体名/字号/字重)。
        /// </summary>
        IPlatformTextFormat CreateTextFormat(
            string fontFamily,
            double fontSize,
            FontWeight fontWeight = FontWeight.Normal,
            FontStyle fontStyle = FontStyle.Normal);
    }
}
