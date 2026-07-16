using System;
using System.Globalization;
using System.Xml;
using SkiaSharp;

namespace Bpf.Windows.Platform
{
    /// <summary>
    /// 极简 SVG 光栅化器。用 System.Xml 解析 SVG,在 SKPictureRecorder 上录制绘图指令,
    /// 得到可任意缩放的 SKPicture,再按目标尺寸光栅化成 SKBitmap。
    ///
    /// 支持的元素:svg/g(分组 + transform)、path、rect、circle、ellipse、line、polyline、polygon。
    /// 支持的属性:fill、fill-opacity、stroke、stroke-width、stroke-opacity、opacity、transform、
    ///   width/height/viewBox(svg 根)、d(path)、points(polyline/polygon)。
    ///
    /// 不依赖任何第三方 SVG 库,纯 SkiaSharp 2.88 + System.Xml,AOT 友好。
    /// 目标是覆盖图标类 SVG(单色/多色、基本形状 + 路径),这是 UI 中最常见的需求。
    /// 复杂特性(滤镜 filter、渐变 gradient、文字 text、clip-path、mask)暂不支持。
    /// </summary>
    internal sealed class SvgDocument : IDisposable
    {
        private readonly SKPicture _picture;
        private readonly float _sourceWidth;
        private readonly float _sourceHeight;

        private SvgDocument(SKPicture picture, float w, float h)
        {
            _picture = picture;
            _sourceWidth = w;
            _sourceHeight = h;
        }

        public int SourceWidth => (int)Math.Ceiling(_sourceWidth);
        public int SourceHeight => (int)Math.Ceiling(_sourceHeight);

        /// <summary>把矢量图按 (width × height) 光栅化成 BGRA 位图。</summary>
        public SKBitmap Rasterize(int width, int height)
        {
            if (width <= 0) width = SourceWidth;
            if (height <= 0) height = SourceHeight;
            if (width <= 0) width = 1;
            if (height <= 0) height = 1;

            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            var bmp = new SKBitmap(info);
            using var canvas = new SKCanvas(bmp);
            canvas.Clear(SKColors.Transparent);
            // 把源 viewBox 缩放到目标尺寸
            float sx = width / _sourceWidth;
            float sy = height / _sourceHeight;
            canvas.Scale(sx, sy);
            canvas.DrawPicture(_picture);
            canvas.Flush();
            return bmp;
        }

        public void Dispose() => _picture.Dispose();

        // ── 解析 ──

        /// <summary>从文件加载并解析 SVG。</summary>
        public static SvgDocument Load(string path)
        {
            var doc = new XmlDocument { XmlResolver = null };
            doc.Load(path);
            return Parse(doc.DocumentElement!);
        }

        private static SvgDocument Parse(XmlElement root)
        {
            // svg 根:解析 width/height/viewBox 确定源尺寸
            float w = ParseLength(root.GetAttribute("width"));
            float h = ParseLength(root.GetAttribute("height"));
            var viewBox = ParseNumbers(root.GetAttribute("viewBox"));
            float vbX = 0, vbY = 0, vbW = 0, vbH = 0;
            if (viewBox.Count >= 4)
            {
                vbX = viewBox[0]; vbY = viewBox[1]; vbW = viewBox[2]; vbH = viewBox[3];
            }
            // 源尺寸优先用 width/height;缺失时用 viewBox 的 w/h
            if (w <= 0 && vbW > 0) w = vbW;
            if (h <= 0 && vbH > 0) h = vbH;
            if (w <= 0) w = 100;
            if (h <= 0) h = 100;

            using var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(new SKRect(0, 0, w, h));
            // 应用 viewBox 偏移:把 viewBox 区域映射到 (0,0,w,h)
            if (vbW > 0 && vbH > 0 && (vbX != 0 || vbY != 0))
                canvas.Translate(-vbX, -vbY);

            var ctx = SvgContext.Default;
            RenderChildren(root, canvas, ctx);
            var picture = recorder.EndRecording();
            return new SvgDocument(picture, w, h);
        }

        // ── 渲染上下文(继承的样式 + 变换) ──

        private struct SvgContext
        {
            public SKColor? Fill;
            public SKColor? Stroke;
            public float StrokeWidth;
            public float Opacity; // 元素自身 opacity(0..1)

            public static SvgContext Default => new SvgContext
            {
                Fill = SKColors.Black,    // SVG 默认 fill=black
                Stroke = null,            // SVG 默认 stroke=none
                StrokeWidth = 1f,
                Opacity = 1f,
            };

            public SvgContext Merge(XmlElement e)
            {
                var c = this;
                var fillAttr = e.GetAttribute("fill");
                if (fillAttr == "none") c.Fill = null;
                else if (fillAttr.Length > 0) c.Fill = ParseColor(fillAttr);
                // fill 继承:不写则保留父级

                var strokeAttr = e.GetAttribute("stroke");
                if (strokeAttr == "none") c.Stroke = null;
                else if (strokeAttr.Length > 0) c.Stroke = ParseColor(strokeAttr);

                var sw = e.GetAttribute("stroke-width");
                if (sw.Length > 0 && float.TryParse(sw, NumberStyles.Float, CultureInfo.InvariantCulture, out var swv))
                    c.StrokeWidth = swv;

                var op = e.GetAttribute("opacity");
                if (op.Length > 0 && float.TryParse(op, NumberStyles.Float, CultureInfo.InvariantCulture, out var opv))
                    c.Opacity = Math.Clamp(opv, 0f, 1f);
                return c;
            }
        }

        private static void RenderChildren(XmlElement parent, SKCanvas canvas, SvgContext ctx)
        {
            foreach (XmlNode n in parent.ChildNodes)
            {
                if (n is XmlElement e) RenderElement(e, canvas, ctx);
            }
        }

        private static void RenderElement(XmlElement e, SKCanvas canvas, SvgContext inherited)
        {
            var ctx = inherited.Merge(e);
            // 处理 transform:translate/scale/rotate/matrix
            var transform = e.GetAttribute("transform");
            int undoCount = 0;
            bool hasTransform = transform.Length > 0 && TryApplyTransform(transform, canvas, out undoCount);

            // opacity < 1 时需要用 SaveLayer 限定透明度范围
            bool useLayer = ctx.Opacity < 1f;
            if (useLayer)
            {
                canvas.SaveLayer();
            }

            switch (e.Name)
            {
                case "svg":
                case "g":
                case "defs":   // defs 内是定义,不直接渲染(本实现忽略 gradient/filter 定义)
                case "symbol":
                    RenderChildren(e, canvas, ctx);
                    break;
                case "path":
                    DrawPath(e, canvas, ctx);
                    break;
                case "rect":
                    DrawRect(e, canvas, ctx);
                    break;
                case "circle":
                    DrawCircle(e, canvas, ctx);
                    break;
                case "ellipse":
                    DrawEllipse(e, canvas, ctx);
                    break;
                case "line":
                    DrawLine(e, canvas, ctx);
                    break;
                case "polyline":
                    DrawPoly(e, canvas, ctx, close: false);
                    break;
                case "polygon":
                    DrawPoly(e, canvas, ctx, close: true);
                    break;
                case "use":
                    // 简化:不支持 use 引用(需要 symbol/defs 解析)。静默跳过。
                    break;
                    // text/filter/image 等暂不支持
            }

            if (useLayer) canvas.Restore();

            if (hasTransform)
            {
                for (int i = 0; i < undoCount; i++) canvas.Restore();
            }
        }

        // ── 各形状绘制 ──

        private static void DrawPath(XmlElement e, SKCanvas canvas, SvgContext ctx)
        {
            var d = e.GetAttribute("d");
            if (d.Length == 0) return;
            var path = PathParser.Parse(d);
            FillAndStroke(canvas, path, ctx);
        }

        private static void DrawRect(XmlElement e, SKCanvas canvas, SvgContext ctx)
        {
            float x = AttrFloat(e, "x"), y = AttrFloat(e, "y");
            float w = AttrFloat(e, "width"), h = AttrFloat(e, "height");
            float rx = AttrFloat(e, "rx"), ry = AttrFloat(e, "ry");
            if (w <= 0 || h <= 0) return;
            var rect = new SKRect(x, y, x + w, y + h);
            if (rx > 0 || ry > 0)
            {
                if (ry <= 0) ry = rx;
                if (rx <= 0) rx = ry;
                var rr = new SKRoundRect(rect, rx, ry);
                FillAndStroke(canvas, rr, ctx);
            }
            else
            {
                using var paint = BuildFillPaint(ctx, out bool hasFill);
                if (hasFill) canvas.DrawRect(rect, paint);
                using var strokePaint = BuildStrokePaint(ctx, out bool hasStroke);
                if (hasStroke) canvas.DrawRect(rect, strokePaint);
            }
        }

        private static void DrawCircle(XmlElement e, SKCanvas canvas, SvgContext ctx)
        {
            float cx = AttrFloat(e, "cx"), cy = AttrFloat(e, "cy"), r = AttrFloat(e, "r");
            if (r <= 0) return;
            DrawEllipseImpl(canvas, ctx, cx - r, cy - r, cx + r, cy + r);
        }

        private static void DrawEllipse(XmlElement e, SKCanvas canvas, SvgContext ctx)
        {
            float cx = AttrFloat(e, "cx"), cy = AttrFloat(e, "cy");
            float rx = AttrFloat(e, "rx"), ry = AttrFloat(e, "ry");
            if (rx <= 0 || ry <= 0) return;
            DrawEllipseImpl(canvas, ctx, cx - rx, cy - ry, cx + rx, cy + ry);
        }

        private static void DrawEllipseImpl(SKCanvas canvas, SvgContext ctx, float left, float top, float right, float bottom)
        {
            var rect = new SKRect(left, top, right, bottom);
            using var fillPaint = BuildFillPaint(ctx, out bool hasFill);
            if (hasFill) canvas.DrawOval(rect, fillPaint);
            using var strokePaint = BuildStrokePaint(ctx, out bool hasStroke);
            if (hasStroke) canvas.DrawOval(rect, strokePaint);
        }

        private static void DrawLine(XmlElement e, SKCanvas canvas, SvgContext ctx)
        {
            float x1 = AttrFloat(e, "x1"), y1 = AttrFloat(e, "y1");
            float x2 = AttrFloat(e, "x2"), y2 = AttrFloat(e, "y2");
            using var strokePaint = BuildStrokePaint(ctx, out bool hasStroke);
            if (hasStroke) canvas.DrawLine(x1, y1, x2, y2, strokePaint);
        }

        private static void DrawPoly(XmlElement e, SKCanvas canvas, SvgContext ctx, bool close)
        {
            var pts = ParseNumbers(e.GetAttribute("points"));
            if (pts.Count < 4) return; // 至少 2 个点
            using var path = new SKPath();
            path.MoveTo(pts[0], pts[1]);
            for (int i = 2; i + 1 < pts.Count; i += 2)
                path.LineTo(pts[i], pts[i + 1]);
            if (close) path.Close();
            FillAndStroke(canvas, path, ctx);
        }

        // ── fill/stroke 应用 ──

        private static void FillAndStroke(SKCanvas canvas, SKPath path, SvgContext ctx)
        {
            using var fillPaint = BuildFillPaint(ctx, out bool hasFill);
            if (hasFill) canvas.DrawPath(path, fillPaint);
            using var strokePaint = BuildStrokePaint(ctx, out bool hasStroke);
            if (hasStroke) canvas.DrawPath(path, strokePaint);
        }

        private static void FillAndStroke(SKCanvas canvas, SKRoundRect rr, SvgContext ctx)
        {
            using var fillPaint = BuildFillPaint(ctx, out bool hasFill);
            if (hasFill) canvas.DrawRoundRect(rr, fillPaint);
            using var strokePaint = BuildStrokePaint(ctx, out bool hasStroke);
            if (hasStroke) canvas.DrawRoundRect(rr, strokePaint);
        }

        private static SKPaint BuildFillPaint(SvgContext ctx, out bool has)
        {
            has = ctx.Fill.HasValue;
            return new SKPaint
            {
                Color = ctx.Fill ?? SKColors.Transparent,
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
            };
        }

        private static SKPaint BuildStrokePaint(SvgContext ctx, out bool has)
        {
            has = ctx.Stroke.HasValue;
            return new SKPaint
            {
                Color = ctx.Stroke ?? SKColors.Transparent,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = ctx.StrokeWidth,
                IsAntialias = true,
            };
        }

        // ── transform 解析 ──

        /// <summary>应用 transform 字符串到 canvas。返回 true 表示有变换(已 Save),调用方需 Restore undoCount 次。</summary>
        private static bool TryApplyTransform(string transform, SKCanvas canvas, out int undoCount)
        {
            canvas.Save();
            undoCount = 1;
            try
            {
                // 支持 transform 链:"translate(10,0) scale(2)"
                int i = 0;
                while (i < transform.Length)
                {
                    // 读函数名
                    int nameStart = i;
                    while (i < transform.Length && char.IsLetter(transform[i])) i++;
                    if (i == nameStart) { i++; continue; }
                    string name = transform.Substring(nameStart, i - nameStart).Trim();
                    // 读括号内参数
                    var args = ReadParenArgs(transform, ref i);
                    if (args == null) continue;
                    switch (name)
                    {
                        case "translate":
                            {
                                float tx = args.Count > 0 ? args[0] : 0;
                                float ty = args.Count > 1 ? args[1] : 0;
                                canvas.Translate(tx, ty);
                            }
                            break;
                        case "scale":
                            {
                                float sx = args.Count > 0 ? args[0] : 1;
                                float sy = args.Count > 1 ? args[1] : sx;
                                canvas.Scale(sx, sy);
                            }
                            break;
                        case "rotate":
                            {
                                float deg = args.Count > 0 ? args[0] : 0;
                                float cx = args.Count > 1 ? args[1] : 0;
                                float cy = args.Count > 2 ? args[2] : 0;
                                if (cx != 0 || cy != 0)
                                {
                                    canvas.Translate(cx, cy);
                                    canvas.RotateDegrees(deg);
                                    canvas.Translate(-cx, -cy);
                                }
                                else
                                {
                                    canvas.RotateDegrees(deg);
                                }
                            }
                            break;
                        case "matrix":
                            {
                                if (args.Count >= 6)
                                {
                                    var m = new SKMatrix(
                                        args[0], args[2], args[4],
                                        args[1], args[3], args[5],
                                        0, 0, 1);
                                    canvas.Concat(ref m);
                                }
                            }
                            break;
                        case "skewX":
                            if (args.Count > 0) canvas.Skew((float)Math.Tan(args[0] * Math.PI / 180), 0);
                            break;
                        case "skewY":
                            if (args.Count > 0) canvas.Skew(0, (float)Math.Tan(args[0] * Math.PI / 180));
                            break;
                    }
                }
                return true;
            }
            catch
            {
                return true; // 即使解析失败也返回 true(已 Save),保证配对 Restore
            }
        }

        private static System.Collections.Generic.List<float>? ReadParenArgs(string s, ref int i)
        {
            // 跳过空白和 '('
            while (i < s.Length && (char.IsWhiteSpace(s[i]) || s[i] == ',')) i++;
            if (i >= s.Length || s[i] != '(') return null;
            i++; // 跳过 '('
            int start = i;
            while (i < s.Length && s[i] != ')') i++;
            if (i >= s.Length) return null;
            string inner = s.Substring(start, i - start);
            i++; // 跳过 ')'
            return ParseNumbers(inner);
        }

        // ── 辅助解析 ──

        private static float AttrFloat(XmlElement e, string name)
        {
            var v = e.GetAttribute(name);
            return v.Length == 0 ? 0f : ParseLength(v);
        }

        /// <summary>解析长度值,去掉单位后缀(px/pt/em 等)。本实现忽略单位差异(按 px 处理)。</summary>
        private static float ParseLength(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = s.Trim();
            // 截断非数字部分
            int end = 0;
            while (end < s.Length && (char.IsDigit(s[end]) || s[end] == '.' || s[end] == '-' || s[end] == '+' || s[end] == 'e' || s[end] == 'E')) end++;
            var num = end > 0 ? s.Substring(0, end) : s;
            return float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }

        private static System.Collections.Generic.List<float> ParseNumbers(string s)
        {
            var result = new System.Collections.Generic.List<float>();
            int i = 0;
            while (i < s.Length)
            {
                while (i < s.Length && !char.IsDigit(s[i]) && s[i] != '.' && s[i] != '-' && s[i] != '+') i++;
                if (i >= s.Length) break;
                int start = i;
                if (s[i] == '-' || s[i] == '+') i++;
                while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'e' || s[i] == 'E')) i++;
                if (i > start)
                {
                    var sub = s.Substring(start, i - start);
                    if (float.TryParse(sub, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                        result.Add(v);
                }
            }
            return result;
        }

        /// <summary>
        /// 解析颜色:#RRGGBB / #RGB / #RRGGBBAA / rgb(r,g,b) / 颜色名(常用)。
        /// </summary>
        private static SKColor ParseColor(string s)
        {
            s = s.Trim();
            if (s.Length == 0) return SKColors.Black;
            if (s[0] == '#')
            {
                string hex = s.Substring(1);
                // 扩展 #RGB → #RRGGBB
                if (hex.Length == 3)
                    hex = new string(new[] { hex[0], hex[0], hex[1], hex[1], hex[2], hex[2] });
                if (hex.Length == 6)
                    return new SKColor((uint)(0xFF000000 | ParseHex(hex)));
                if (hex.Length == 8)
                    return new SKColor(ParseHex(hex));
                return SKColors.Black;
            }
            if (s.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            {
                var nums = ParseNumbers(s);
                if (nums.Count >= 3)
                {
                    byte r = (byte)Math.Clamp(nums[0], 0, 255);
                    byte g = (byte)Math.Clamp(nums[1], 0, 255);
                    byte b = (byte)Math.Clamp(nums[2], 0, 255);
                    return new SKColor(r, g, b);
                }
                return SKColors.Black;
            }
            // 常见颜色名
            return s.ToLowerInvariant() switch
            {
                "black" => SKColors.Black,
                "white" => SKColors.White,
                "red" => SKColors.Red,
                "green" => SKColors.Green,
                "blue" => SKColors.Blue,
                "yellow" => SKColors.Yellow,
                "cyan" => SKColors.Cyan,
                "magenta" => SKColors.Magenta,
                "gray" or "grey" => SKColors.Gray,
                "orange" => SKColors.Orange,
                "purple" => SKColors.Purple,
                "pink" => SKColors.Pink,
                "brown" => SKColors.Brown,
                "none" => SKColors.Transparent,
                _ => SKColors.Black,
            };
        }

        private static uint ParseHex(string hex)
        {
            uint val = 0;
            foreach (char c in hex)
            {
                val <<= 4;
                if (c >= '0' && c <= '9') val |= (uint)(c - '0');
                else if (c >= 'a' && c <= 'f') val |= (uint)(c - 'a' + 10);
                else if (c >= 'A' && c <= 'F') val |= (uint)(c - 'A' + 10);
            }
            return val;
        }
    }
}
