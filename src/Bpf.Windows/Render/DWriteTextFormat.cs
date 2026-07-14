using System;
using System.Runtime.InteropServices;
using Bpf;
using Bpf.Windows.Interop;

namespace Bpf.Windows.Render
{
    /// <summary>
    /// 包装 IDWriteTextFormat。用于度量单行文本。
    /// </summary>
    internal sealed unsafe class DWriteTextFormat : IDisposable
    {
        private IntPtr _format;

        // IDWriteTextFormat vtable:
        //   0 QI / 1 AddRef / 2 Release
        //   3 SetTextAlignment
        //   4 SetParagraphAlignment
        //   5 SetWordWrapping
        //   6 SetReadingDirection
        //   7 SetFlowDirection
        //   8 SetIncrementalTabStop
        //   9 SetTrimming
        //   10 SetLineSpacing
        //   11 GetTextAlignment
        //   12 GetParagraphAlignment
        //   13 GetWordWrapping
        //   14 GetReadingDirection
        //   15 GetFlowDirection
        //   16 GetIncrementalTabStop
        //   17 GetTrimming
        //   18 GetLineSpacing
        //   19 GetFontCollection
        //   20 GetFontFamilyNameLength
        //   21 GetFontFamilyName
        //   22 GetFontWeight
        //   23 GetFontStyle
        //   24 GetFontStretch
        //   25 GetFontSize
        //   26 GetLocaleNameLength
        //   27 GetLocaleName
        public IntPtr NativePtr => _format;

        public string FontFamily { get; }
        public float FontSize { get; }

        public DWriteTextFormat(IntPtr native, string fontFamily, float fontSize)
        {
            _format = native;
            FontFamily = fontFamily;
            FontSize = fontSize;
        }

        // IDWriteTextLayout 继承 IUnknown(3) + IDWriteTextFormat(25)= 槽 0-27,
        // 自身 33 个方法到 GetMetrics(SetMaxWidth=28..GetMetrics=60)。
        // GetMetrics 是自身第 33 个,绝对槽位 = 3 + 25 + (33-1) = 60。
        private const int Slt_TextLayout_GetMetrics = 60;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void GetMetricsDelegate(IntPtr thisPtr, DWrite.DWRITE_TEXT_METRICS* metrics);

        /// <summary>
        /// 用一个临时 TextLayout 度量文本。返回 (width, height)。
        /// </summary>
        public static Size Measure(DWriteFactory factory, IntPtr textFormatPtr, string text)
        {
            var layoutPtr = factory.CreateTextLayout(
                text, textFormatPtr,
                float.MaxValue, float.MaxValue);
            try
            {
                var getMetrics = Com.GetVTableMethod<GetMetricsDelegate>(layoutPtr, Slt_TextLayout_GetMetrics);
                DWrite.DWRITE_TEXT_METRICS metrics;
                getMetrics(layoutPtr, &metrics);
                return new Size(metrics.widthIncludingTrailingWhitespace, metrics.height);
            }
            finally
            {
                Com.Release(layoutPtr);
            }
        }

        public void Dispose()
        {
            if (_format != IntPtr.Zero)
            {
                Com.Release(_format);
                _format = IntPtr.Zero;
            }
        }
    }
}
