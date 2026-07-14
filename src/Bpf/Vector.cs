using System;

namespace Bpf
{
    /// <summary>
    /// 二维向量。值类型,不可变。API 风格向 Avalonia 的 Vector 靠拢。
    /// </summary>
    public readonly struct Vector : IEquatable<Vector>
    {
        public double X { get; }
        public double Y { get; }

        public Vector(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Vector Zero => default;

        public double Length => Math.Sqrt(X * X + Y * Y);

        public static Vector operator +(Vector a, Vector b) => new Vector(a.X + b.X, a.Y + b.Y);
        public static Vector operator -(Vector a, Vector b) => new Vector(a.X - b.X, a.Y - b.Y);
        public static Vector operator -(Vector v) => new Vector(-v.X, -v.Y);
        public static Vector operator *(Vector v, double k) => new Vector(v.X * k, v.Y * k);
        public static Vector operator *(double k, Vector v) => new Vector(v.X * k, v.Y * k);
        public static Vector operator /(Vector v, double k) => new Vector(v.X / k, v.Y / k);

        public static bool operator ==(Vector left, Vector right) => left.Equals(right);
        public static bool operator !=(Vector left, Vector right) => !left.Equals(right);

        public bool Equals(Vector other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object? obj) => obj is Vector v && Equals(v);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"{X}, {Y}";

        public void Deconstruct(out double x, out double y)
        {
            x = X;
            y = Y;
        }
    }
}
