using System;
using System.Runtime.InteropServices;

namespace Bpf.Windows.Interop
{
    /// <summary>
    /// DirectWrite 最小 COM interop。
    /// 仅暴露 M1 所需的 IDWriteFactory::CreateTextFormat + IDWriteTextFormat 的度量方法。
    /// </summary>
    internal static unsafe class DWrite
    {
        // DWRITE_FACTORY_TYPE
        public const int DWRITE_FACTORY_TYPE_SHARED = 0;
        public const int DWRITE_FACTORY_TYPE_ISOLATED = 1;

        // DWRITE_FONT_WEIGHT
        public const int DWRITE_FONT_WEIGHT_NORMAL = 400;
        public const int DWRITE_FONT_WEIGHT_BOLD = 700;

        // DWRITE_FONT_STYLE
        public const int DWRITE_FONT_STYLE_NORMAL = 0;
        public const int DWRITE_FONT_STYLE_ITALIC = 1;
        public const int DWRITE_FONT_STYLE_OBLIQUE = 2;

        // DWRITE_FONT_STRETCH
        public const int DWRITE_FONT_STRETCH_NORMAL = 5;

        // DWRITE_TEXT_ALIGNMENT
        public const int DWRITE_TEXT_ALIGNMENT_LEADING = 1;

        // DWRITE_PARAGRAPH_ALIGNMENT
        public const int DWRITE_PARAGRAPH_ALIGNMENT_NEAR = 0;

        [DllImport("dwrite.dll", EntryPoint = "DWriteCreateFactory", CallingConvention = CallingConvention.StdCall)]
        public static extern int DWriteCreateFactory(
            int factoryType,
            ref Guid iid,
            out IntPtr ppFactory);

        [StructLayout(LayoutKind.Sequential)]
        public struct DWRITE_TEXT_METRICS
        {
            public float left;
            public float top;
            public float width;
            public float widthIncludingTrailingWhitespace;
            public float height;
            public float layoutWidth;
            public float layoutHeight;
            public int maxBidiRelevanceDepth;
            public int lineCount;
        }
    }
}
