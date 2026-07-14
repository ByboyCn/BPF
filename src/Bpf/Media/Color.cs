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
    }
}
