using System;
using System.Runtime.InteropServices;
using Bpf.Windows.Interop;

namespace Bpf.Windows.Render
{
    /// <summary>
    /// 包装 ID2D1HwndRenderTarget。提供 BeginDraw/EndDraw/Clear/各种 Draw/Fill/
    /// 文本绘制、裁剪、Resize。仅暴露 M1 所需方法。
    /// </summary>
    /// <remarks>
    /// vtable 槽位(绝对值,来自 d2d1.h 的 ID2D1RenderTarget : ID2D1Resource : IUnknown):
    ///   0 QI / 1 AddRef / 2 Release / 3 GetFactory
    ///   4 CreateBitmap / 5 CreateBitmapFromWicBitmap / 6 CreateSharedBitmap
    ///   7 CreateBitmapBrush / 8 CreateSolidColorBrush
    ///   9 CreateGradientStopCollection / 10 CreateLinearGradientBrush / 11 CreateRadialGradientBrush
    ///   12 CreateCompatibleRenderTarget / 13 CreateLayer / 14 CreateMesh
    ///   15 DrawLine / 16 DrawRectangle / 17 FillRectangle
    ///   18 DrawRoundedRectangle / 19 FillRoundedRectangle
    ///   20 DrawEllipse / 21 FillEllipse / 22 DrawGeometry / 23 FillGeometry
    ///   24 FillMesh / 25 FillOpacityMask / 26 DrawBitmap
    ///   27 DrawText / 28 DrawTextLayout / 29 DrawGlyphRun
    ///   30 SetTransform / 31 GetTransform / 32-39 Antialias/Tags ...
    ///   40 PushLayer / 41 PopLayer / 42 Flush / 43 SaveDrawingState / 44 RestoreDrawingState
    ///   45 PushAxisAlignedClip / 46 PopAxisAlignedClip
    ///   47 Clear / 48 BeginDraw / 49 EndDraw
    ///   50 GetPixelFormat / 51 SetDpi / 52 GetDpi / 53 GetSize / 54 GetPixelSize
    ///   55 GetMaximumBitmapSize / 56 IsSupported
    /// ID2D1HwndRenderTarget 自身(继承全部上述):
    ///   57 CheckWindowState / 58 Resize / 59 GetHwnd / 60 GetDC / 61 ReleaseDC / 62 BindDC / 63 ReloadSystemMetrics
    /// 改动槽位时务必比对 d2d1.h,顺序错位会直接崩。
    /// </remarks>
    internal sealed unsafe class D2D1RenderTarget : IDisposable
    {
        private IntPtr _rt;

        // ── 委托声明(与 COM 函数签名一致,首个参数为 thisPtr) ──

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateSolidColorBrushDelegate(
            IntPtr thisPtr,
            D2D1.D2D1_COLOR_F* color,
            IntPtr brushProperties, // 可空,D2D1_BRUSH_PROPERTIES*。M1 不传 (NULL)
            out IntPtr brush);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateSolidColorBrushWithPropsDelegate(
            IntPtr thisPtr,
            D2D1.D2D1_COLOR_F* color,
            D2D1.D2D1_BRUSH_PROPERTIES* brushProperties,
            out IntPtr brush);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void DrawTextDelegate(
            IntPtr thisPtr,
            [MarshalAs(UnmanagedType.LPWStr)] string text,
            uint textLength,
            IntPtr textFormat,
            D2D1.D2D1_RECT_F* layoutRect,
            IntPtr defaultFillBrush,
            int options,
            int measuringMode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void DrawRectangleDelegate(
            IntPtr thisPtr, D2D1.D2D1_RECT_F* rect, IntPtr brush,
            float strokeWidth, IntPtr strokeStyle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void DrawRoundedRectangleDelegate(
            IntPtr thisPtr, D2D1.D2D1_ROUNDED_RECT* roundedRect, IntPtr brush,
            float strokeWidth, IntPtr strokeStyle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void FillRectangleDelegate(
            IntPtr thisPtr, D2D1.D2D1_RECT_F* rect, IntPtr brush);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void FillRoundedRectangleDelegate(
            IntPtr thisPtr, D2D1.D2D1_ROUNDED_RECT* roundedRect, IntPtr brush);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void BeginDrawDelegate(IntPtr thisPtr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int EndDrawDelegate(
            IntPtr thisPtr, out long tag1, out long tag2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void ClearDelegate(IntPtr thisPtr, D2D1.D2D1_COLOR_F* clearColor);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void PushClipDelegate(
            IntPtr thisPtr, D2D1.D2D1_RECT_F* clipRect, int antialiasMode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void PopClipDelegate(IntPtr thisPtr);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void SetTransformDelegate(IntPtr thisPtr, D2D1.D2D1_MATRIX_3X2_F* transform);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void GetSizeDelegate(IntPtr thisPtr, out float width, out float height);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int ResizeDelegate(IntPtr thisPtr, D2D1.D2D1_SIZE_U* pixelSize);

        public D2D1RenderTarget(IntPtr nativeRt)
        {
            _rt = nativeRt;
        }

        public IntPtr NativePtr => _rt;

        public IntPtr CreateSolidColorBrush(D2D1.D2D1_COLOR_F color)
        {
            var create = Com.GetVTableMethod<CreateSolidColorBrushDelegate>(_rt, 8);
            int hr = create(_rt, &color, IntPtr.Zero, out var brush);
            if (hr != 0)
                throw new InvalidOperationException($"CreateSolidColorBrush 失败,hr=0x{hr:X8}");
            return brush;
        }

        public void BeginDraw()
        {
            var begin = Com.GetVTableMethod<BeginDrawDelegate>(_rt, 48);
            begin(_rt);
        }

        public void EndDraw()
        {
            var end = Com.GetVTableMethod<EndDrawDelegate>(_rt, 49);
            int hr = end(_rt, out _, out _);
            if (hr != 0)
                throw new InvalidOperationException($"EndDraw 失败,hr=0x{hr:X8}");
        }

        public void Clear(D2D1.D2D1_COLOR_F color)
        {
            var clear = Com.GetVTableMethod<ClearDelegate>(_rt, 47);
            clear(_rt, &color);
        }

        public void FillRectangle(D2D1.D2D1_RECT_F rect, IntPtr brush)
        {
            var fill = Com.GetVTableMethod<FillRectangleDelegate>(_rt, 17);
            fill(_rt, &rect, brush);
        }

        public void DrawRectangle(D2D1.D2D1_RECT_F rect, IntPtr brush, float strokeWidth)
        {
            var draw = Com.GetVTableMethod<DrawRectangleDelegate>(_rt, 16);
            draw(_rt, &rect, brush, strokeWidth, IntPtr.Zero);
        }

        public void FillRoundedRectangle(D2D1.D2D1_ROUNDED_RECT roundedRect, IntPtr brush)
        {
            var fill = Com.GetVTableMethod<FillRoundedRectangleDelegate>(_rt, 19);
            fill(_rt, &roundedRect, brush);
        }

        public void DrawRoundedRectangle(D2D1.D2D1_ROUNDED_RECT roundedRect, IntPtr brush, float strokeWidth)
        {
            var draw = Com.GetVTableMethod<DrawRoundedRectangleDelegate>(_rt, 18);
            draw(_rt, &roundedRect, brush, strokeWidth, IntPtr.Zero);
        }

        public void DrawText(
            string text,
            IntPtr textFormat,
            D2D1.D2D1_RECT_F layoutRect,
            IntPtr fillBrush)
        {
            var draw = Com.GetVTableMethod<DrawTextDelegate>(_rt, 27);
            draw(_rt, text, (uint)text.Length, textFormat,
                &layoutRect, fillBrush,
                D2D1.D2D1_DRAW_TEXT_OPTIONS_NONE,
                0); // D2D1_MEASURING_MODE_NATURAL
        }

        public void PushClip(D2D1.D2D1_RECT_F clipRect)
        {
            var push = Com.GetVTableMethod<PushClipDelegate>(_rt, 45);
            push(_rt, &clipRect, 1); // D2D1_ANTIALIAS_MODE_PER_PRIMITIVE
        }

        public void PopClip()
        {
            var pop = Com.GetVTableMethod<PopClipDelegate>(_rt, 46);
            pop(_rt);
        }

        /// <summary>设置纯平移变换(覆盖式)。调用方负责维护变换栈语义。</summary>
        public void SetTransformTranslate(float tx, float ty)
        {
            var m = new D2D1.D2D1_MATRIX_3X2_F
            {
                _11 = 1, _12 = 0,
                _21 = 0, _22 = 1,
                _31 = tx, _32 = ty,
            };
            var set = Com.GetVTableMethod<SetTransformDelegate>(_rt, 30);
            set(_rt, &m);
        }

        public void GetSize(out float width, out float height)
        {
            var get = Com.GetVTableMethod<GetSizeDelegate>(_rt, 53);
            get(_rt, out width, out height);
        }

        public void Resize(uint width, uint height)
        {
            var size = new D2D1.D2D1_SIZE_U { width = width, height = height };
            var resize = Com.GetVTableMethod<ResizeDelegate>(_rt, 58);
            int hr = resize(_rt, &size);
            // hr 非零可能是尺寸为 0 等情况,不抛异常以容错
        }

        public void Dispose()
        {
            if (_rt != IntPtr.Zero)
            {
                Com.Release(_rt);
                _rt = IntPtr.Zero;
            }
        }
    }
}
