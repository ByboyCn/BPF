using System;

namespace Bpf.Platform
{
    /// <summary>
    /// 平台原生画笔句柄。后端自行包装其具体实现(D2D1 的 ID2D1Brush 等)。
    /// 控件不应直接持有本接口,应持有 <see cref="Bpf.Media.Brush"/>。
    /// </summary>
    public interface IPlatformBrush : IDisposable
    {
    }

    /// <summary>
    /// 平台原生文本格式。后端包装(DWrite 的 IDWriteTextFormat 等)。
    /// </summary>
    public interface IPlatformTextFormat : IDisposable
    {
        string FontFamily { get; }
        double FontSize { get; }
        FontWeight FontWeight { get; }
        FontStyle FontStyle { get; }

        /// <summary>
        /// 度量指定文本在该格式下的自然尺寸(DIP)。后端用 DWrite/SK 等实现。
        /// </summary>
        Size MeasureText(string text);
    }
}
