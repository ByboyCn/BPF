using System;

namespace Bpf
{
    /// <summary>
    /// 二维点。值类型,不可变。
    /// </summary>
    public readonly struct Point : IEquatable<Point>
    {
        public double X { get; }
        public double Y { get; }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Point Origin => default;

        public Point WithX(double x) => new Point(x, Y);
        public Point WithY(double y) => new Point(X, y);

        public static Point operator +(Point p, Vector v) => new Point(p.X + v.X, p.Y + v.Y);
        public static Point operator -(Point p, Vector v) => new Point(p.X - v.X, p.Y - v.Y);
        public static Vector operator -(Point a, Point b) => new Vector(a.X - b.X, a.Y - b.Y);

        public static bool operator ==(Point left, Point right) => left.Equals(right);
        public static bool operator !=(Point left, Point right) => !left.Equals(right);

        public bool Equals(Point other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object? obj) => obj is Point p && Equals(p);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"{X}, {Y}";

        public void Deconstruct(out double x, out double y)
        {
            x = X;
            y = Y;
        }
    }
}
