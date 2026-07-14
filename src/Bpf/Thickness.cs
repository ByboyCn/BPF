using System;

namespace Bpf
{
    /// <summary>
    /// 描述矩形四周的间距(边距/边框/填充)。值类型,不可变。
    /// </summary>
    public readonly struct Thickness : IEquatable<Thickness>
    {
        public double Left { get; }
        public double Top { get; }
        public double Right { get; }
        public double Bottom { get; }

        public Thickness(double uniformLength)
        {
            Left = Top = Right = Bottom = uniformLength;
        }

        public Thickness(double horizontal, double vertical)
        {
            Left = Right = horizontal;
            Top = Bottom = vertical;
        }

        public Thickness(double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public static Thickness Empty => default;

        public bool IsEmpty => Left == 0 && Top == 0 && Right == 0 && Bottom == 0;

        public double Horizontal => Left + Right;
        public double Vertical => Top + Bottom;

        public bool IsUniform => Left.Equals(Top) && Top.Equals(Right) && Right.Equals(Bottom);

        public static Thickness operator +(Thickness a, Thickness b) =>
            new Thickness(a.Left + b.Left, a.Top + b.Top, a.Right + b.Right, a.Bottom + b.Bottom);

        public static Thickness operator -(Thickness a, Thickness b) =>
            new Thickness(a.Left - b.Left, a.Top - b.Top, a.Right - b.Right, a.Bottom - b.Bottom);

        public static bool operator ==(Thickness left, Thickness right) => left.Equals(right);
        public static bool operator !=(Thickness left, Thickness right) => !left.Equals(right);

        public bool Equals(Thickness other) =>
            Left.Equals(other.Left) && Top.Equals(other.Top) &&
            Right.Equals(other.Right) && Bottom.Equals(other.Bottom);

        public override bool Equals(object? obj) => obj is Thickness t && Equals(t);

        public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

        public override string ToString() => $"{Left},{Top},{Right},{Bottom}";
    }
}
