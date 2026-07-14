using System;

namespace Bpf
{
    /// <summary>
    /// 矩形。以左上角 + 宽高表示。值类型,不可变。
    /// </summary>
    public readonly struct Rect : IEquatable<Rect>
    {
        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }

        public Rect(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Rect(Point position, Size size)
        {
            X = position.X;
            Y = position.Y;
            Width = size.Width;
            Height = size.Height;
        }

        public Rect(Point topLeft, Point bottomRight)
        {
            X = Math.Min(topLeft.X, bottomRight.X);
            Y = Math.Min(topLeft.Y, bottomRight.Y);
            Width = Math.Abs(bottomRight.X - topLeft.X);
            Height = Math.Abs(bottomRight.Y - topLeft.Y);
        }

        public static Rect Empty => default;

        public Point Position => new Point(X, Y);
        public Size Size => new Size(Width, Height);

        public Point TopLeft => new Point(X, Y);
        public Point TopRight => new Point(X + Width, Y);
        public Point BottomLeft => new Point(X, Y + Height);
        public Point BottomRight => new Point(X + Width, Y + Height);

        public double Left => X;
        public double Top => Y;
        public double Right => X + Width;
        public double Bottom => Y + Height;

        public bool IsEmpty => Width <= 0 || Height <= 0;

        public Point Center => new Point(X + Width / 2, Y + Height / 2);

        public Rect WithX(double x) => new Rect(x, Y, Width, Height);
        public Rect WithY(double y) => new Rect(X, y, Width, Height);
        public Rect WithWidth(double width) => new Rect(X, Y, width, Height);
        public Rect WithHeight(double height) => new Rect(X, Y, Width, height);
        public Rect WithPosition(Point p) => new Rect(p, Size);
        public Rect WithSize(Size s) => new Rect(Position, s);

        /// <summary>
        /// 将矩形平移。
        /// </summary>
        public Rect Offset(Vector v) => new Rect(X + v.X, Y + v.Y, Width, Height);

        public Rect Translate(double dx, double dy) => new Rect(X + dx, Y + dy, Width, Height);

        /// <summary>
        /// 按四边内缩(inflate 为负则外扩)。
        /// </summary>
        public Rect Deflate(Thickness t) =>
            new Rect(
                X + t.Left,
                Y + t.Top,
                Math.Max(0, Width - t.Left - t.Right),
                Math.Max(0, Height - t.Top - t.Bottom));

        /// <summary>
        /// 按四边外扩(负则内缩)。
        /// </summary>
        public Rect Inflate(Thickness t) =>
            new Rect(
                X - t.Left,
                Y - t.Top,
                Width + t.Left + t.Right,
                Height + t.Top + t.Bottom);

        /// <summary>
        /// 求两矩形并集(覆盖两者的最小矩形)。
        /// </summary>
        public Rect Union(Rect other)
        {
            var x = Math.Min(X, other.X);
            var y = Math.Min(Y, other.Y);
            var right = Math.Max(Right, other.Right);
            var bottom = Math.Max(Bottom, other.Bottom);
            return new Rect(x, y, right - x, bottom - y);
        }

        /// <summary>
        /// 判断点是否在矩形内。
        /// </summary>
        public bool Contains(Point p) =>
            p.X >= X && p.X <= X + Width && p.Y >= Y && p.Y <= Y + Height;

        /// <summary>
        /// 判断是否包含另一矩形。
        /// </summary>
        public bool Contains(Rect r) =>
            X <= r.X && Y <= r.Y && Right >= r.Right && Bottom >= r.Bottom;

        public static Rect operator +(Rect r, Vector v) => r.Offset(v);
        public static Rect operator -(Rect r, Vector v) => r.Offset(-v);

        public static bool operator ==(Rect left, Rect right) => left.Equals(right);
        public static bool operator !=(Rect left, Rect right) => !left.Equals(right);

        public bool Equals(Rect other) =>
            X.Equals(other.X) && Y.Equals(other.Y) &&
            Width.Equals(other.Width) && Height.Equals(other.Height);

        public override bool Equals(object? obj) => obj is Rect r && Equals(r);

        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public override string ToString() => $"{X},{Y},{Width},{Height}";
    }
}
