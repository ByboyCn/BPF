using System;
using Bpf.Media;
using Bpf.Platform;
using Bpf.PropertySystem;

namespace Bpf.Controls
{
    /// <summary>
    /// 图片控件。从文件路径加载 PNG/JPEG/BMP/GIF 并显示(SkiaSharp 解码)。
    /// </summary>
    public sealed class Image : Control
    {
        public static readonly StyledProperty<string> SourceProperty =
            StyledProperty<string>.Register<Image>(nameof(Source), "",
                affectsMeasure: true, affectsRender: true);

        public string Source
        {
            get => GetValue(SourceProperty);
            set
            {
                SetValue(SourceProperty, value);
                _bitmap?.Dispose();
                _bitmap = null;
            }
        }

        public static readonly StyledProperty<Stretch> StretchProperty =
            StyledProperty<Stretch>.Register<Image>(nameof(Stretch),
                Stretch.Uniform, affectsRender: true);

        public Stretch Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        private IPlatformBitmap? _bitmap;

        private IPlatformBitmap? EnsureBitmap()
        {
            if (_bitmap is not null) return _bitmap;
            if (string.IsNullOrEmpty(Source)) return null;
            if (HostWindow is null) return null;
            try
            {
                _bitmap = Bpf.Application.Application.Current.RenderInterface.LoadBitmap(Source);
            }
            catch { _bitmap = null; }
            return _bitmap;
        }

        protected override Size MeasureCore(Size availableSize)
        {
            var bmp = EnsureBitmap();
            if (bmp is null)
                return new Size(
                    Math.Min(availableSize.Width, 64),
                    Math.Min(availableSize.Height, 64));
            return new Size(
                Math.Min(availableSize.Width == double.PositiveInfinity ? bmp.PixelWidth : availableSize.Width, bmp.PixelWidth),
                Math.Min(availableSize.Height == double.PositiveInfinity ? bmp.PixelHeight : availableSize.Height, bmp.PixelHeight));
        }

        protected override void ArrangeCore(Rect finalRect) => Bounds = finalRect;

        public override void Render(IDrawingContext context)
        {
            var bmp = EnsureBitmap();
            if (bmp is not null)
            {
                var destRect = ComputeStretchRect(Stretch, bmp.PixelWidth, bmp.PixelHeight,
                    Bounds.Width, Bounds.Height);
                context.DrawImage(bmp, destRect);
            }
            else
            {
                var render = Bpf.Application.Application.Current.RenderInterface;
                var brush = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)).ToPlatform(render);
                try { context.FillRectangle(new Rect(0, 0, Bounds.Width, Bounds.Height), brush); }
                finally { brush.Dispose(); }
            }
        }

        private static Rect ComputeStretchRect(Stretch stretch, int imgW, int imgH, double destW, double destH)
        {
            if (imgW <= 0 || imgH <= 0) return new Rect(0, 0, destW, destH);
            switch (stretch)
            {
                case Stretch.Fill:
                    return new Rect(0, 0, destW, destH);
                case Stretch.Uniform:
                    {
                        double scale = Math.Min(destW / imgW, destH / imgH);
                        double w = imgW * scale, h = imgH * scale;
                        return new Rect((destW - w) / 2, (destH - h) / 2, w, h);
                    }
                case Stretch.UniformToFill:
                    {
                        double scale = Math.Max(destW / imgW, destH / imgH);
                        double w = imgW * scale, h = imgH * scale;
                        return new Rect((destW - w) / 2, (destH - h) / 2, w, h);
                    }
                default:
                    return new Rect(0, 0, Math.Min(imgW, destW), Math.Min(imgH, destH));
            }
        }

        public override bool HitTest(Point point) => IsVisible && Bounds.Contains(point);
    }
}
