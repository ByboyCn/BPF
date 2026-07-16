using System;

namespace Bpf.Media
{
    /// <summary>
    /// 表示 sRGB 颜色。值类型,不可变。
    /// </summary>
    public readonly struct Color : IEquatable<Color>
    {
        public byte A { get; }
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public Color(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        public static Color FromRgb(byte r, byte g, byte b) => new Color(255, r, g, b);
        public static Color FromArgb(byte a, byte r, byte g, byte b) => new Color(a, r, g, b);

        public static Color FromUInt32(uint value) =>
            new Color(
                (byte)((value >> 24) & 0xFF),
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)(value & 0xFF));

        public uint ToUInt32() =>
            ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | B;

        // 预定义常用颜色
        public static Color Transparent => new Color(0, 0, 0, 0);
        public static Color Black => FromRgb(0, 0, 0);
        public static Color White => FromRgb(255, 255, 255);
        public static Color Red => FromRgb(255, 0, 0);
        public static Color Green => FromRgb(0, 128, 0);
        public static Color Blue => FromRgb(0, 0, 255);
        public static Color LightGray => FromRgb(211, 211, 211);
        public static Color Gray => FromRgb(128, 128, 128);
        public static Color DarkGray => FromRgb(64, 64, 64);

        public static bool operator ==(Color left, Color right) => left.Equals(right);
        public static bool operator !=(Color left, Color right) => !left.Equals(right);

        public bool Equals(Color other) =>
            A == other.A && R == other.R && G == other.G && B == other.B;

        public override bool Equals(object? obj) => obj is Color c && Equals(c);

        public override int GetHashCode() => HashCode.Combine(A, R, G, B);

        public override string ToString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

        /// <summary>
        /// 从字符串解析颜色。支持:
        ///   #RGB、#RRGGBB、#AARRGGBB(十六进制,大小写不敏感)
        ///   颜色名(Black/White/Red/Green/Blue/Gray/LightGray/DarkGray/Transparent,大小写不敏感)
        /// .bpfaml 标记语言用它把 Background="Red"/"#FF8800" 转成 Color。
        /// </summary>
        public static Color Parse(string s)
        {
            if (s is null) throw new ArgumentNullException(nameof(s));
            s = s.Trim();
            if (s.Length == 0) throw new FormatException("空颜色字符串");

            if (s[0] == '#')
            {
                string hex = s.Substring(1);
                // 扩展 #RGB → #RRGGBB
                if (hex.Length == 3)
                    hex = new string(new[] { hex[0], hex[0], hex[1], hex[1], hex[2], hex[2] });
                if (hex.Length == 6)
                    return FromRgb(HexByte(hex, 0), HexByte(hex, 2), HexByte(hex, 4));
                if (hex.Length == 8)
                    return FromArgb(HexByte(hex, 0), HexByte(hex, 2), HexByte(hex, 4), HexByte(hex, 6));
                throw new FormatException($"无效颜色格式: {s}(支持 #RGB/#RRGGBB/#AARRGGBB)");
            }

            // 颜色名
            switch (s.ToLowerInvariant())
            {
                case "transparent": return Transparent;
                case "black": return Black;
                case "white": return White;
                case "red": return Red;
                case "green": return Green;
                case "blue": return Blue;
                case "lightgray": return LightGray;
                case "gray": case "grey": return Gray;
                case "darkgray": return DarkGray;
                default:
                    throw new FormatException($"未知颜色名: {s}");
            }
        }

        /// <summary>尝试解析,失败返回 false(不抛异常)。</summary>
        public static bool TryParse(string s, out Color color)
        {
            try { color = Parse(s); return true; }
            catch { color = default; return false; }
        }

        private static byte HexByte(string hex, int offset)
        {
            int hi = HexVal(hex[offset]);
            int lo = HexVal(hex[offset + 1]);
            if (hi < 0 || lo < 0) throw new FormatException($"无效十六进制: {hex}");
            return (byte)((hi << 4) | lo);
        }

        private static int HexVal(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }
    }
}
