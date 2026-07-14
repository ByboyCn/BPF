using System;
using System.Collections.Generic;
using Bpf;
using Bpf.Media;
using Bpf.Platform;
using Bpf.Windows.Interop;
using Bpf.Windows.Render;

namespace Bpf.Windows.Platform
{
    /// <summary>
    /// IDrawingContext 的 Windows/D2D1 实现。把核心层几何/画笔翻译为 D2D1 调用。
    /// </summary>
    internal sealed class Win32DrawingContext : IDrawingContext
    {
        private readonly Win32RenderTarget _owner;
        private readonly D2D1RenderTarget _rt;
        private bool _disposed;
        private readonly Stack<D2D1.D2D1_RECT_F> _clipStack = new Stack<D2D1.D2D1_RECT_F>();
        // 变换栈:D2D1 的 SetTransform 是覆盖式的,这里手工维护累积平移以支持嵌套。
        private double _accumTx;
        private double _accumTy;
        private readonly Stack<(double, double)> _transformStack = new Stack<(double, double)>();

        public Win32DrawingContext(Win32RenderTarget owner, D2D1RenderTarget rt)
        {
            _owner = owner;
            _rt = rt;
        }

        public void Clear(Color color)
        {
            _rt.Clear(ToD2DColor(color));
        }

        public void FillRectangle(Rect rect, IPlatformBrush brush)
        {
            var ptr = Materialize(brush);
            _rt.FillRectangle(ToD2DRect(rect), ptr);
        }

        public void DrawRectangle(Rect rect, IPlatformBrush brush, double strokeWidth)
        {
            var ptr = Materialize(brush);
            _rt.DrawRectangle(ToD2DRect(rect), ptr, (float)strokeWidth);
        }

        public void DrawRoundedRectangle(Rect rect, double radiusX, double radiusY,
            IPlatformBrush brush, double strokeWidth)
        {
            var ptr = Materialize(brush);
            var rr = new D2D1.D2D1_ROUNDED_RECT
            {
                rect = ToD2DRect(rect),
                radiusX = (float)radiusX,
                radiusY = (float)radiusY,
            };
            _rt.DrawRoundedRectangle(rr, ptr, (float)strokeWidth);
        }

        public void FillRoundedRectangle(Rect rect, double radiusX, double radiusY,
            IPlatformBrush brush)
        {
            var ptr = Materialize(brush);
            var rr = new D2D1.D2D1_ROUNDED_RECT
            {
                rect = ToD2DRect(rect),
                radiusX = (float)radiusX,
                radiusY = (float)radiusY,
            };
            _rt.FillRoundedRectangle(rr, ptr);
        }

        public void DrawText(Point origin, string text, IPlatformTextFormat format,
            IPlatformBrush foreground)
        {
            if (format is Win32TextFormat wtf)
            {
                var brushPtr = Materialize(foreground);
                var layoutRect = new D2D1.D2D1_RECT_F
                {
                    left = (float)origin.X,
                    top = (float)origin.Y,
                    right = (float)origin.X + 10000f,
                    bottom = (float)origin.Y + 1000f,
                };
                _rt.DrawText(text, wtf.NativePtr, layoutRect, brushPtr);
            }
        }

        public Size MeasureText(string text, IPlatformTextFormat format)
        {
            if (format is Win32TextFormat wtf)
                return wtf.MeasureText(text);
            return Size.Empty;
        }

        public void PushClip(Rect clip)
        {
            _rt.PushClip(ToD2DRect(clip));
            _clipStack.Push(ToD2DRect(clip));
        }

        public void PopClip()
        {
            if (_clipStack.Count > 0)
            {
                _clipStack.Pop();
                _rt.PopClip();
            }
        }

        public void PushTranslate(Vector offset)
        {
            // D2D1 的 SetTransform 是覆盖式的,不是栈。手工维护累积平移以支持嵌套。
            _transformStack.Push((_accumTx, _accumTy));
            _accumTx += offset.X;
            _accumTy += offset.Y;
            _rt.SetTransformTranslate((float)_accumTx, (float)_accumTy);
        }

        public void PopTransform()
        {
            if (_transformStack.Count > 0)
            {
                var prev = _transformStack.Pop();
                _accumTx = prev.Item1;
                _accumTy = prev.Item2;
                _rt.SetTransformTranslate((float)_accumTx, (float)_accumTy);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _owner.EndDraw();
            }
        }

        // ── 辅助 ──

        private IntPtr Materialize(IPlatformBrush brush)
        {
            if (brush is Win32DeferredBrush db)
                return _owner.MaterializeBrush(db);
            throw new InvalidOperationException(
                $"未知的画笔类型:{brush?.GetType().FullName}");
        }

        private static D2D1.D2D1_COLOR_F ToD2DColor(Color c) => new D2D1.D2D1_COLOR_F
        {
            r = c.R / 255f,
            g = c.G / 255f,
            b = c.B / 255f,
            a = c.A / 255f,
        };

        private static D2D1.D2D1_RECT_F ToD2DRect(Rect r) => new D2D1.D2D1_RECT_F
        {
            left = (float)r.X,
            top = (float)r.Y,
            right = (float)(r.X + r.Width),
            bottom = (float)(r.Y + r.Height),
        };
    }
}
