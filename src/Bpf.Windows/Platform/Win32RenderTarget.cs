using System;
using System.Collections.Generic;
using Bpf;
using Bpf.Platform;
using Bpf.Windows.Interop;
using Bpf.Windows.Render;

namespace Bpf.Windows.Platform
{
    /// <summary>
    /// IRenderTarget 的 Windows/D2D1 实现。
    /// 包装 ID2D1HwndRenderTarget。处理 D2DERR_RECREATE_TARGET(设备丢失时重建)。
    /// </summary>
    internal sealed unsafe class Win32RenderTarget : IRenderTarget
    {
        // D2DERR_RECREATE_TARGET:渲染目标需要重建(设备丢失等)。
        private const int D2DERR_RECREATE_TARGET = unchecked((int)0x88990001);

        private readonly D2D1Factory _factory;
        private readonly DWriteFactory _dwrite;
        private readonly IntPtr _hwnd;
        private D2D1RenderTarget? _rt;
        private Size _size;
        private double _scaling = 1.0;
        private readonly List<Win32DeferredBrush> _brushes = new List<Win32DeferredBrush>();
        private bool _needsRecreate;

        public Win32RenderTarget(D2D1Factory factory, DWriteFactory dwrite, IntPtr hwnd)
        {
            _factory = factory;
            _dwrite = dwrite;
            _hwnd = hwnd;
            RecreateRenderTarget();
        }

        /// <summary>(重新)创建 HWND 渲染目标。首次创建和 D2DERR_RECREATE_TARGET 后调用。</summary>
        private void RecreateRenderTarget()
        {
            // 释放旧 RT 及其物化的 brush
            _rt?.Dispose();
            foreach (var b in _brushes) b.Invalidate();
            _rt = null;

            // 用客户区像素尺寸创建
            User32.GetClientRect(_hwnd, out var rc);
            uint w = (uint)Math.Max(1, rc.Right - rc.Left);
            uint h = (uint)Math.Max(1, rc.Bottom - rc.Top);
            _size = new Size(w, h);

            var nativePtr = _factory.CreateHwndRenderTarget(_hwnd, w, h);
            _rt = new D2D1RenderTarget(nativePtr);

            // DPI 缩放
            int dpi = User32.GetDpiForWindow(_hwnd);
            if (dpi > 0) _scaling = dpi / 96.0;

            _needsRecreate = false;
        }

        public Size PixelSize => _size;

        public Size DipSize => new Size(_size.Width / _scaling, _size.Height / _scaling);

        public double Scaling => _scaling;

        public D2D1RenderTarget Native => _rt ?? throw new ObjectDisposedException(nameof(Win32RenderTarget));

        public DWriteFactory DWrite => _dwrite;

        public void Resize(Size size)
        {
            _size = size;
            _rt?.Resize((uint)Math.Max(1, size.Width), (uint)Math.Max(1, size.Height));
            // brush 还有效(同一 RenderTarget)
        }

        public IDrawingContext BeginDraw()
        {
            // 若上一帧标记需重建,先重建 RT
            if (_needsRecreate || _rt is null)
            {
                RecreateRenderTarget();
            }
            _rt!.BeginDraw();
            return new Win32DrawingContext(this, _rt);
        }

        public void Present()
        {
            // HWND RenderTarget 在 EndDraw 时自动 present,这里无需额外操作。
        }

        internal void EndDraw()
        {
            if (_rt is null) return;
            try
            {
                _rt.EndDraw();
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("88990001") || ex.Message.Contains("RECREATE"))
            {
                // D2DERR_RECREATE_TARGET:设备丢失,标记下帧重建。
                // 这是 D2D1 HWND RT 的正常情况,文档要求丢弃 RT 及其资源后重建。
                _needsRecreate = true;
            }
        }

        public IntPtr MaterializeBrush(Win32DeferredBrush brush)
        {
            if (!_brushes.Contains(brush)) _brushes.Add(brush);
            return brush.Materialize(_rt!);
        }

        public void Dispose()
        {
            // 释放 brush(实际 native brush 由 RT 释放,只需清引用)
            _brushes.Clear();
            _rt?.Dispose();
            _rt = null;
        }
    }
}
