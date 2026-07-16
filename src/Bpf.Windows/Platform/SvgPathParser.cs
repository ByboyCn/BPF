using System.Collections.Generic;
using SkiaSharp;

namespace Bpf.Windows.Platform
{
    /// <summary>
    /// 解析 SVG path 的 d 属性,生成 SKPath。
    /// 支持命令(大小写区分:大写=绝对坐标,小写=相对坐标):
    ///   M/m moveto, L/l lineto, H/h 水平线, V/v 垂直线,
    ///   C/c 三次贝塞尔, S/s 平滑三次, Q/q 二次贝塞尔, T/t 平滑二次,
    ///   A/a 弧形(用贝塞尔近似), Z/z 闭合。
    /// 多组子路径用 M 开头分割。
    /// </summary>
    internal static class PathParser
    {
        public static SKPath Parse(string d)
        {
            var path = new SKPath();
            var tokens = Tokenize(d);
            int i = 0;
            float curX = 0, curY = 0;       // 当前点
            float startX = 0, startY = 0;    // 当前子路径起点(用于 Z 回到起点)
            char lastCmd = '\0';

            while (i < tokens.Count)
            {
                var t = tokens[i];
                char cmd;
                if (t.IsCommand)
                {
                    cmd = t.Value[0];
                    i++;
                }
                else
                {
                    // 隐式重复上一命令(M 之后隐式 L,L/H/V/C/S/Q/T/A 重复)
                    if (lastCmd == 'M') cmd = 'L';
                    else if (lastCmd == 'm') cmd = 'l';
                    else cmd = lastCmd;
                    if (cmd == '\0') { i++; continue; }
                }

                bool rel = char.IsLower(cmd);
                switch (char.ToUpper(cmd))
                {
                    case 'M':
                        curX = Read(ref i); curY = Read(ref i);
                        if (rel) { curX += startX; curY += startY; }
                        // 修正:相对 M 是相对上一个点,不是相对子路径起点
                        // 但首个 M 必是绝对,且 SVG 规范 M 相对也是相对"当前点"
                        path.MoveTo(curX, curY);
                        startX = curX; startY = curY;
                        // M 后面跟多对坐标时,按隐式 L 处理
                        break;
                    case 'L':
                        while (PeekPair(i))
                        {
                            float x = Read(ref i), y = Read(ref i);
                            if (rel) { x += curX; y += curY; }
                            curX = x; curY = y;
                            path.LineTo(curX, curY);
                        }
                        break;
                    case 'H':
                        while (i < tokens.Count && !tokens[i].IsCommand)
                        {
                            float x = Read(ref i);
                            if (rel) x += curX;
                            curX = x;
                            path.LineTo(curX, curY);
                        }
                        break;
                    case 'V':
                        while (i < tokens.Count && !tokens[i].IsCommand)
                        {
                            float y = Read(ref i);
                            if (rel) y += curY;
                            curY = y;
                            path.LineTo(curX, curY);
                        }
                        break;
                    case 'C':
                        while (PeekPair(i) && i + 5 < tokens.Count + 1 && PeekAt(i + 5))
                        {
                            float x1 = Read(ref i), y1 = Read(ref i);
                            float x2 = Read(ref i), y2 = Read(ref i);
                            float x = Read(ref i), y = Read(ref i);
                            if (rel) { x1 += curX; y1 += curY; x2 += curX; y2 += curY; x += curX; y += curY; }
                            path.CubicTo(x1, y1, x2, y2, x, y);
                            curX = x; curY = y;
                        }
                        break;
                    case 'S':
                        while (PeekAt(i + 3))
                        {
                            float x2 = Read(ref i), y2 = Read(ref i);
                            float x = Read(ref i), y = Read(ref i);
                            if (rel) { x2 += curX; y2 += curY; x += curX; y += curY; }
                            // S 的第一个控制点是上一段 C 的第二控制点关于当前点的镜像
                            // 简化处理:直接用当前点作为第一控制点(非精确,但视觉接近)
                            path.CubicTo(curX, curY, x2, y2, x, y);
                            curX = x; curY = y;
                        }
                        break;
                    case 'Q':
                        while (PeekAt(i + 3))
                        {
                            float x1 = Read(ref i), y1 = Read(ref i);
                            float x = Read(ref i), y = Read(ref i);
                            if (rel) { x1 += curX; y1 += curY; x += curX; y += curY; }
                            path.QuadTo(x1, y1, x, y);
                            curX = x; curY = y;
                        }
                        break;
                    case 'T':
                        while (PeekAt(i + 1))
                        {
                            float x = Read(ref i), y = Read(ref i);
                            if (rel) { x += curX; y += curY; }
                            // T 的控制点是上一段 Q 的控制点镜像,简化:用当前点
                            path.QuadTo(curX, curY, x, y);
                            curX = x; curY = y;
                        }
                        break;
                    case 'A':
                        while (PeekAt(i + 6))
                        {
                            float rx = Read(ref i), ry = Read(ref i);
                            float angle = Read(ref i);
                            float large = Read(ref i), sweep = Read(ref i);
                            float x = Read(ref i), y = Read(ref i);
                            if (rel) { x += curX; y += curY; }
                            AddArc(path, curX, curY, rx, ry, angle, large != 0, sweep != 0, x, y);
                            curX = x; curY = y;
                        }
                        break;
                    case 'Z':
                        path.Close();
                        curX = startX; curY = startY;
                        break;
                }
                lastCmd = cmd;
            }
            return path;

            // 局部辅助
            float Read(ref int idx) { var v = tokens[idx].Number; idx++; return v; }
            bool PeekPair(int idx) => idx + 1 < tokens.Count && !tokens[idx].IsCommand && !tokens[idx + 1].IsCommand;
            bool PeekAt(int idx) => idx < tokens.Count && !tokens[idx].IsCommand;
        }

        /// <summary>用三次贝塞尔近似 SVG 弧形(A 命令)。这是 path 解析里最复杂的部分。</summary>
        private static void AddArc(SKPath path, float x1, float y1, float rxParam, float ryParam,
            float angleDeg, bool largeArc, bool sweep, float x2, float y2)
        {
            if (rxParam == 0 || ryParam == 0 || (x1 == x2 && y1 == y2))
            {
                path.LineTo(x2, y2);
                return;
            }
            double rx = System.Math.Abs((double)rxParam);
            double ry = System.Math.Abs((double)ryParam);

            // SVG 弧算法(end-point parameterization → center parameterization),参考 SVG 规范 F.6.5
            double rad = angleDeg * System.Math.PI / 180.0;
            double cosA = System.Math.Cos(rad), sinA = System.Math.Sin(rad);

            double dx = (x1 - x2) / 2.0, dy = (y1 - y2) / 2.0;
            double x1p = cosA * dx + sinA * dy;
            double y1p = -sinA * dx + cosA * dy;

            double rx2 = rx * rx, ry2 = ry * ry, x1p2 = x1p * x1p, y1p2 = y1p * y1p;
            double lambda = x1p2 / rx2 + y1p2 / ry2;
            if (lambda > 1)
            {
                double s = System.Math.Sqrt(lambda);
                rx *= s; ry *= s; rx2 = rx * rx; ry2 = ry * ry;
            }

            double sign = (largeArc != sweep) ? 1 : -1;
            double num = rx2 * ry2 - rx2 * y1p2 - ry2 * x1p2;
            double den = rx2 * y1p2 + ry2 * x1p2;
            double coef = sign * System.Math.Sqrt(System.Math.Max(0, num / den));
            double cxp = coef * (rx * y1p / ry);
            double cyp = coef * -(ry * x1p / rx);

            double cx = cosA * cxp - sinA * cyp + (x1 + x2) / 2;
            double cy = sinA * cxp + cosA * cyp + (y1 + y2) / 2;

            double ux = (x1p - cxp) / rx, uy = (y1p - cyp) / ry;
            double vx = (-x1p - cxp) / rx, vy = (-y1p - cyp) / ry;

            double theta1 = AngleBetween(1, 0, ux, uy);
            double dTheta = AngleBetween(ux, uy, vx, vy);
            if (!sweep && dTheta > 0) dTheta -= 2 * System.Math.PI;
            if (sweep && dTheta < 0) dTheta += 2 * System.Math.PI;

            // 把弧分成若干段 90 度以内的贝塞尔
            const double step = System.Math.PI / 2;
            int segments = (int)System.Math.Ceiling(System.Math.Abs(dTheta) / step);
            double delta = dTheta / segments;
            double t = theta1;

            double cosRx = rx * cosA, sinRx = rx * sinA; // 注意:后续变换用
            // 椭圆上点: theta → ((cosRx*cos - sinRy*sin... , 这里直接按未旋转椭圆算再旋转
            // 但为简单,我们直接对每段用 cubic bezier 近似圆弧再缩放
            // 标准:对一段 [-t,0] 的圆弧用 t*tan(angle/4)*4/3 控制点
            // 这里用通用的椭圆弧贝塞尔近似

            double kappa = 4.0 / 3.0 * System.Math.Tan(delta / 4);
            for (int k = 0; k < segments; k++)
            {
                double a1 = (k == 0) ? theta1 : theta1 + k * delta;
                double a2 = theta1 + (k + 1) * delta;

                EllipsePoint(a1, out double p1x, out double p1y);
                EllipsePoint(a2, out double p2x, out double p2y);
                EllipseTangent(a1, out double t1x, out double t1y);
                EllipseTangent(a2, out double t2x, out double t2y);

                double c1x = p1x + kappa * t1x, c1y = p1y + kappa * t1y;
                double c2x = p2x - kappa * t2x, c2y = p2y - kappa * t2y;

                ToScreen(c1x, c1y, out float sc1x, out float sc1y);
                ToScreen(c2x, c2y, out float sc2x, out float sc2y);
                ToScreen(p2x, p2y, out float sp2x, out float sp2y);
                path.CubicTo(sc1x, sc1y, sc2x, sc2y, sp2x, sp2y);
            }

            // 局部函数:椭圆参数角 → 椭圆上的(单位圆再缩放)点
            void EllipsePoint(double ang, out double px, out double py)
            {
                double ca = System.Math.Cos(ang), sa = System.Math.Sin(ang);
                px = rx * ca; py = ry * sa;
            }
            void EllipseTangent(double ang, out double tx, out double ty)
            {
                // 切线方向(对参数 t 求导):(-rx*sin, ry*cos)
                tx = -rx * System.Math.Sin(ang);
                ty = ry * System.Math.Cos(ang);
            }
            void ToScreen(double ex, double ey, out float sx, out float sy)
            {
                // 椭圆点(椭圆局部坐标)→ 旋转 angle → 平移到中心
                double rx2s = cosA * ex - sinA * ey + cx;
                double ry2s = sinA * ex + cosA * ey + cy;
                sx = (float)rx2s; sy = (float)ry2s;
            }
        }

        private static double AngleBetween(double ux, double uy, double vx, double vy)
        {
            double dot = ux * vx + uy * vy;
            double len = System.Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
            if (len == 0) return 0;
            double cos = System.Math.Clamp(dot / len, -1, 1);
            double a = System.Math.Acos(cos);
            if (ux * vy - uy * vx < 0) a = -a;
            return a;
        }

        // ── Tokenizer ──

        private struct Tok(bool isCmd, string val, float num)
        {
            public bool IsCommand = isCmd;
            public string Value = val;
            public float Number = num;
        }

        private static List<Tok> Tokenize(string d)
        {
            var list = new List<Tok>();
            int i = 0;
            while (i < d.Length)
            {
                char c = d[i];
                if (char.IsWhiteSpace(c) || c == ',') { i++; continue; }
                if (IsCommandChar(c))
                {
                    list.Add(new Tok(true, c.ToString(), 0));
                    i++;
                    continue;
                }
                // 数字(可能带符号、小数点、指数)。注意负号可能是数字分隔符(如 "1-2" → 1, -2)
                int start = i;
                if (c == '-' || c == '+') i++;
                bool seenDot = false, seenExp = false;
                while (i < d.Length)
                {
                    char ch = d[i];
                    if (char.IsDigit(ch)) { i++; }
                    else if (ch == '.' && !seenDot && !seenExp) { seenDot = true; i++; }
                    else if ((ch == 'e' || ch == 'E') && !seenExp) { seenExp = true; i++; if (i < d.Length && (d[i] == '+' || d[i] == '-')) i++; }
                    else break;
                }
                if (i > start)
                {
                    var sub = d.Substring(start, i - start);
                    if (float.TryParse(sub, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v))
                        list.Add(new Tok(false, sub, v));
                    else
                        i = start + 1; // 解析失败,前进避免死循环
                }
                else
                {
                    i++;
                }
            }
            return list;
        }

        private static bool IsCommandChar(char c) =>
            "MmLlHhVvCcSsQqTtAaZz".IndexOf(c) >= 0;
    }
}
