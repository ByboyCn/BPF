using System.Collections.Generic;
using Bpf;
using Bpf.Media;
using Bpf.Platform;
using SkiaSharp;

namespace Bpf.Windows.Platform
{
    /// <summary>
    /// IDrawingContext 的 SkiaSharp 实现。用 SKCanvas 绘制,替代 D2D1 COM vtable。
    /// </summary>
    internal sealed class SkiaDrawingContext : IDrawingContext
    {
        private readonly SKCanvas _canvas;
        private bool _disposed;

        public SkiaDrawingContext(SKCanvas canvas)
        {
            _canvas = canvas;
        }

        public void Clear(Color color)
        {
            _canvas.Clear(ToSKColor(color));
        }

        public void FillRectangle(Rect rect, IPlatformBrush brush)
        {
            using var paint = new SKPaint
            {
                Color = GetBrushColor(brush),
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
            };
            _canvas.DrawRect(ToSKRect(rect), paint);
        }

        public void DrawRectangle(Rect rect, IPlatformBrush brush, double strokeWidth)
        {
            using var paint = new SKPaint
            {
                Color = GetBrushColor(brush),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = (float)strokeWidth,
            };
            _canvas.DrawRect(ToSKRect(rect), paint);
        }

        public void DrawRoundedRectangle(Rect rect, double radiusX, double radiusY,
            IPlatformBrush brush, double strokeWidth)
        {
            using var paint = new SKPaint
            {
                Color = GetBrushColor(brush),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = (float)strokeWidth,
            };
            var rrect = new SKRoundRect(ToSKRect(rect), (float)radiusX, (float)radiusY);
            _canvas.DrawRoundRect(rrect, paint);
        }

        public void FillRoundedRectangle(Rect rect, double radiusX, double radiusY,
            IPlatformBrush brush)
        {
            using var paint = new SKPaint
            {
                Color = GetBrushColor(brush),
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
            };
            var rrect = new SKRoundRect(ToSKRect(rect), (float)radiusX, (float)radiusY);
            _canvas.DrawRoundRect(rrect, paint);
        }

        public void DrawText(Point origin, string text, IPlatformTextFormat format,
            IPlatformBrush foreground)
        {
            if (format is SkiaTextFormat stf)
            {
                // SkiaSharp 的 DrawText(text, x, y) 中 y 是 baseline 坐标。
                // 我们的 origin 是文字框的左上角(行高顶部),baseline = origin.Y + |Ascent|。
                // 用 FontMetrics.Ascent(负值)计算精确 baseline,而非固定的 0.85f 系数。
                float startX = (float)origin.X;
                float startY = (float)origin.Y;

                // 段间共用一个 paint,切换 Typeface + TextSize,横向累进绘制
                using var paint = new SKPaint
                {
                    Color = GetBrushColor(foreground),
                    IsAntialias = true,
                    TextSize = (float)stf.FontSize,
                };

                foreach (var (segment, isCjk) in stf.SplitByGlyphCoverage(text))
                {
                    paint.Typeface = isCjk ? stf.CjkTypeface : stf.Typeface;
                    // 每段按自己的 Ascent 计算该段 baseline,让基线对齐
                    var m = paint.FontMetrics;
                    float baseline = startY + (-m.Ascent);
                    _canvas.DrawText(segment, startX, baseline, paint);
                    startX += paint.MeasureText(segment); // 累进到下一段起点
                }
            }
        }

        public Size MeasureText(string text, IPlatformTextFormat format)
        {
            if (format is SkiaTextFormat stf)
                return stf.MeasureText(text);
            return Size.Empty;
        }

        public void DrawImage(IPlatformBitmap bitmap, Rect destRect)
        {
            if (bitmap is SkiaBitmap sb)
            {
                _canvas.DrawImage(sb.SKImage, ToSKRect(destRect));
            }
        }

        public void PushClip(Rect clip)
        {
            _canvas.Save();
            _canvas.ClipRect(ToSKRect(clip));
        }

        public void PopClip()
        {
            _canvas.Restore();
        }

        public void PushTranslate(Vector offset)
        {
            _canvas.Save();
            _canvas.Translate((float)offset.X, (float)offset.Y);
        }

        public void PopTransform()
        {
            _canvas.Restore();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        // ── 辅助 ──

        private static SKColor GetBrushColor(IPlatformBrush brush)
        {
            if (brush is SkiaSolidBrush sb)
                return sb.Color;
            return new SKColor(0, 0, 0);
        }

        internal static SKColor ToSKColor(Color c) => new SKColor((byte)c.R, (byte)c.G, (byte)c.B, (byte)c.A);

        internal static SKRect ToSKRect(Rect r) => new SKRect(
            (float)r.X, (float)r.Y, (float)(r.X + r.Width), (float)(r.Y + r.Height));
    }
}
