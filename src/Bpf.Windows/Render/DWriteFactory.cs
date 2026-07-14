using System;
using System.Runtime.InteropServices;
using Bpf.Windows.Interop;

namespace Bpf.Windows.Render
{
    /// <summary>
    /// 包装 IDWriteFactory。提供 CreateTextFormat。
    /// </summary>
    internal sealed unsafe class DWriteFactory : IDisposable
    {
        private IntPtr _factory;

        // IDWriteFactory vtable(来自 dwrite.h,继承 IUnknown):
        //   0 QI / 1 AddRef / 2 Release
        //   3 GetSystemFontCollection
        //   4 CreateCustomFontCollection / 5 RegisterFontCollectionLoader / 6 UnregisterFontCollectionLoader
        //   7 CreateFontFileReference / 8 CreateCustomFontFileReference / 9 CreateFontFace
        //   10 CreateRenderingParams / 11 CreateMonitorRenderingParams / 12 CreateCustomRenderingParams
        //   13 RegisterFontFileLoader / 14 UnregisterFontFileLoader
        //   15 CreateTextFormat  ★
        //   16 CreateTypography / 17 GetGdiInterop
        //   18 CreateTextLayout  ★
        private const int Slt_CreateTextFormat = 15;
        private const int Slt_CreateTextLayout = 18;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateTextFormatDelegate(
            IntPtr thisPtr,
            [MarshalAs(UnmanagedType.LPWStr)] string fontFamilyName,
            IntPtr fontCollection, // 可空 (NULL = 系统字体集合)
            int fontWeight,
            int fontStyle,
            int fontStretch,
            float fontSize,
            [MarshalAs(UnmanagedType.LPWStr)] string localeName,
            out IntPtr textFormat);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateTextLayoutDelegate(
            IntPtr thisPtr,
            [MarshalAs(UnmanagedType.LPWStr)] string text,
            uint textLength,
            IntPtr textFormat,
            float maxWidth,
            float maxHeight,
            out IntPtr textLayout);

        public DWriteFactory()
        {
            var iid = Iid.IDWriteFactory;
            int hr = DWrite.DWriteCreateFactory(
                DWrite.DWRITE_FACTORY_TYPE_SHARED,
                ref iid,
                out _factory);
            if (hr != 0 || _factory == IntPtr.Zero)
                throw new InvalidOperationException($"DWriteCreateFactory 失败,hr=0x{hr:X8}");
        }

        public IntPtr NativePtr => _factory;

        /// <summary>创建 IDWriteTextFormat。</summary>
        public IntPtr CreateTextFormat(
            string fontFamily,
            int fontWeight,
            int fontStyle,
            float fontSize)
        {
            var create = Com.GetVTableMethod<CreateTextFormatDelegate>(_factory, Slt_CreateTextFormat);
            int hr = create(_factory, fontFamily, IntPtr.Zero,
                fontWeight, fontStyle,
                DWrite.DWRITE_FONT_STRETCH_NORMAL,
                fontSize,
                "en-us",
                out var format);
            if (hr != 0)
                throw new InvalidOperationException(
                    $"CreateTextFormat 失败,hr=0x{hr:X8},font={fontFamily}");
            return format;
        }

        /// <summary>创建一个临时 TextLayout 用于度量(度量后由调用方释放)。</summary>
        public IntPtr CreateTextLayout(
            string text, IntPtr textFormat, float maxWidth, float maxHeight)
        {
            var create = Com.GetVTableMethod<CreateTextLayoutDelegate>(_factory, Slt_CreateTextLayout);
            int hr = create(_factory, text, (uint)text.Length, textFormat,
                maxWidth, maxHeight, out var layout);
            if (hr != 0)
                throw new InvalidOperationException($"CreateTextLayout 失败,hr=0x{hr:X8}");
            return layout;
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
