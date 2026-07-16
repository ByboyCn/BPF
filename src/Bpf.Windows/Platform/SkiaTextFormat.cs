using System;
using System.Collections.Generic;
using Bpf;
using Bpf.Media;
using Bpf.Platform;
using SkiaSharp;

namespace Bpf.Windows.Platform
{
    /// <summary>IPlatformBrush 的 SkiaSharp 实现。纯色画笔。</summary>
    internal sealed class SkiaSolidBrush : IPlatformBrush
    {
        public SKColor Color { get; }
        public SkiaSolidBrush(Color color)
        {
            Color = new SKColor((byte)color.R, (byte)color.G, (byte)color.B, (byte)color.A);
        }
        public void Dispose() { }
    }

    /// <summary>
    /// IPlatformTextFormat 的 SkiaSharp 实现。用 SKPaint.MeasureText。
    /// 关键:SkiaSharp 与 DWrite 不同,设置 Typeface 后 <b>不做字体回退</b>。
    /// 指定字体(如 Segoe UI)不含中文字形时,中文会显示为方块(.notdef)。
    /// 因此这里维护一个 CJK 回退 Typeface,DrawText 时按字符分段绘制。
    /// </summary>
    internal sealed class SkiaTextFormat : IPlatformTextFormat
    {
        public SKTypeface Typeface { get; }
        /// <summary>CJK 回退字体(用于主字体不含的中文/日文/韩文字符)。可能与 Typeface 相同。</summary>
        public SKTypeface CjkTypeface { get; }
        public string FontFamily { get; }
        public double FontSize { get; }
        public FontWeight FontWeight { get; }
        public FontStyle FontStyle { get; }

        public SkiaTextFormat(string fontFamily, double fontSize, FontWeight fontWeight, FontStyle fontStyle)
        {
            FontFamily = fontFamily;
            FontSize = fontSize;
            FontWeight = fontWeight;
            FontStyle = fontStyle;

            var weight = (SKFontStyleWeight)(int)fontWeight;
            var slant = fontStyle switch
            {
                Bpf.Platform.FontStyle.Italic => SKFontStyleSlant.Italic,
                Bpf.Platform.FontStyle.Oblique => SKFontStyleSlant.Oblique,
                _ => SKFontStyleSlant.Upright,
            };

            // 主字体:指定字体 → Arial → Default
            Typeface = SKTypeface.FromFamilyName(fontFamily, weight, SKFontStyleWidth.Normal, slant)
                       ?? SKTypeface.FromFamilyName("Arial", weight, SKFontStyleWidth.Normal, slant)
                       ?? SKTypeface.Default;

            // CJK 回退字体。SkiaSharp 设置 Typeface 后不做字体回退,所以必须显式找一个含
            // 中日韩字形的字体。优先按已知家族名查找(最可靠,Windows 必装),再用 MatchCharacter 兜底。
            CjkTypeface = SKTypeface.FromFamilyName("Microsoft YaHei", weight, SKFontStyleWidth.Normal, slant)
                          ?? SKTypeface.FromFamilyName("微软雅黑", weight, SKFontStyleWidth.Normal, slant)
                          ?? SKTypeface.FromFamilyName("SimSun", weight, SKFontStyleWidth.Normal, slant)
                          ?? SKTypeface.FromFamilyName("宋体", weight, SKFontStyleWidth.Normal, slant)
                          ?? SKFontManager.Default.MatchCharacter(
                                null, weight, SKFontStyleWidth.Normal, slant,
                                new[] { "zh-Hans", "zh" }, '一')
                          ?? SKFontManager.Default.MatchCharacter('一')
                          ?? Typeface;
        }

        public Size MeasureText(string text)
        {
            // 混合文本需要分段度量(主字体段 + CJK 段),宽度累加,高度取较大者
            float totalWidth = 0f;
            float maxAscent = 0f;
            float maxDescent = 0f;

            foreach (var (segment, isCjk) in SplitByGlyphCoverage(text))
            {
                using var paint = new SKPaint
                {
                    TextSize = (float)FontSize,
                    Typeface = isCjk ? CjkTypeface : Typeface,
                    IsAntialias = true,
                };
                totalWidth += paint.MeasureText(segment);
                var m = paint.FontMetrics;
                if (m.Ascent < maxAscent) maxAscent = m.Ascent;   // Ascent 为负
                if (m.Descent > maxDescent) maxDescent = m.Descent; // Descent 为正
            }
            return new Size(totalWidth, maxDescent - maxAscent);
        }

        /// <summary>
        /// 按字形覆盖情况把文本切分为 (segment, isCjk) 段。
        /// CJK / 全角 / CJK 标点字符走 CjkTypeface,其余走主 Typeface。
        /// 这样英文用主字体(如 Segoe UI),中文用 CJK 字体(如微软雅黑),各自呈现最佳外观。
        /// </summary>
        public IEnumerable<(string text, bool isCjk)> SplitByGlyphCoverage(string text)
        {
            if (string.IsNullOrEmpty(text))
                yield break;

            var sb = new System.Text.StringBuilder();
            bool curCjk = NeedsCjk(text[0]);
            foreach (char c in text)
            {
                bool isCjk = NeedsCjk(c);
                if (isCjk != curCjk && sb.Length > 0)
                {
                    yield return (sb.ToString(), curCjk);
                    sb.Clear();
                }
                sb.Append(c);
                curCjk = isCjk;
            }
            if (sb.Length > 0)
                yield return (sb.ToString(), curCjk);
        }

        /// <summary>判断字符是否属于 CJK / 全角 / CJK 标点范围,需要用 CJK 字体渲染。</summary>
        private static bool NeedsCjk(char c)
        {
            // CJK 统一表意文字(常用汉字)
            if (c >= 0x4E00 && c <= 0x9FFF) return true;
            // CJK 扩展 A
            if (c >= 0x3400 && c <= 0x4DBF) return true;
            // CJK 兼容表意文字
            if (c >= 0xF900 && c <= 0xFAFF) return true;
            // CJK 部首 / 康熙字典部首
            if (c >= 0x2E80 && c <= 0x2EFF) return true;
            // CJK 标点符号(、。「」【】 等)
            if (c >= 0x3000 && c <= 0x303F) return true;
            // 全角字符(全角数字、字母、标点)
            if (c >= 0xFF00 && c <= 0xFFEF) return true;
            // 日文平假名 / 片假名
            if (c >= 0x3040 && c <= 0x30FF) return true;
            // 韩文音节
            if (c >= 0xAC00 && c <= 0xD7AF) return true;
            return false;
        }

        public void Dispose()
        {
            Typeface?.Dispose();
            // CjkTypeface 可能与 Typeface 相同,避免重复释放
            if (!ReferenceEquals(CjkTypeface, Typeface))
                CjkTypeface?.Dispose();
        }
    }

    /// <summary>
    /// IPlatformBitmap 的 SkiaSharp 实现。
    /// 位图(PNG/JPEG/BMP/GIF)用 SKBitmap.Decode;SVG 用 SvgDocument 光栅化。
    /// </summary>
    internal sealed class SkiaBitmap : IPlatformBitmap
    {
        public SKImage SKImage { get; }
        public int PixelWidth { get; }
        public int PixelHeight { get; }

        public SkiaBitmap(string path)
        {
            var ext = System.IO.Path.GetExtension(path);
            if (string.Equals(ext, ".svg", System.StringComparison.OrdinalIgnoreCase))
            {
                // SVG:矢量图,按源 viewBox 尺寸光栅化(后续 Image 控件的 Stretch 会处理缩放显示)。
                // 为了图标清晰,按源尺寸的 2x 光栅化(类似 retina),避免放大模糊。
                using var svg = SvgDocument.Load(path);
                using var bmp = svg.Rasterize(svg.SourceWidth * 2, svg.SourceHeight * 2);
                SKImage = SKImage.FromBitmap(bmp.Copy());
                // 记录源尺寸(非光栅尺寸)给 Image 控件做布局度量
                PixelWidth = svg.SourceWidth;
                PixelHeight = svg.SourceHeight;
            }
            else
            {
                using var bmp = SKBitmap.Decode(path);
                if (bmp is null)
                    throw new InvalidOperationException($"无法解码图片: {path}");
                // 转为 BGRA 格式统一
                if (bmp.ColorType != SKColorType.Bgra8888)
                {
                    var converted = new SKBitmap(bmp.Width, bmp.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                    using var canvas = new SKCanvas(converted);
                    canvas.DrawBitmap(bmp, 0, 0);
                    bmp.Dispose();
                    SKImage = SKImage.FromBitmap(converted);
                }
                else
                {
                    SKImage = SKImage.FromBitmap(bmp.Copy());
                }
                PixelWidth = SKImage.Width;
                PixelHeight = SKImage.Height;
            }
        }

        public void Dispose() => SKImage?.Dispose();
    }
}
