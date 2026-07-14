using System;

namespace Bpf
{
    /// <summary>
    /// 尺寸。宽高均非负。值类型,不可变。
    /// </summary>
    public readonly struct Size : IEquatable<Size>
    {
        public double Width { get; }
        public double Height { get; }

        public Size(double width, double height)
        {
            if (width < 0 || double.IsNaN(width))
                throw new ArgumentException("Size.Width 必须为非负数(可为 Infinity 表示无约束)。", nameof(width));
            if (height < 0 || double.IsNaN(height))
                throw new ArgumentException("Size.Height 必须为非负数(可为 Infinity 表示无约束)。", nameof(height));
            Width = width;
            Height = height;
        }

        public static Size Empty => default;
        public static Size Infinity => new Size(double.PositiveInfinity, double.PositiveInfinity);

        public bool IsEmpty => Width == 0 && Height == 0;

        public Size WithWidth(double width) => new Size(width, Height);
        public Size WithHeight(double height) => new Size(Width, height);

        public static Size operator +(Size size, Thickness t) =>
            new Size(size.Width + t.Left + t.Right, size.Height + t.Top + t.Bottom);

        public static Size operator -(Size size, Thickness t) =>
            new Size(
                Math.Max(0, size.Width - (t.Left + t.Right)),
                Math.Max(0, size.Height - (t.Top + t.Bottom)));

        public static explicit operator Vector(Size s) => new Vector(s.Width, s.Height);
        public static explicit operator Size(Vector v) => new Size(v.X, v.Y);

        public static bool operator ==(Size left, Size right) => left.Equals(right);
        public static bool operator !=(Size left, Size right) => !left.Equals(right);

        public bool Equals(Size other) => Width.Equals(other.Width) && Height.Equals(other.Height);
        public override bool Equals(object? obj) => obj is Size s && Equals(s);

        public override int GetHashCode() => HashCode.Combine(Width, Height);

        public override string ToString() => $"{Width}, {Height}";

        /// <summary>
        /// 取本尺寸与约束尺寸中较小的分量(用于布局裁剪)。
        /// </summary>
        public Size Constrain(Size constraint) =>
            new Size(
                Math.Min(Width, constraint.Width),
                Math.Min(Height, constraint.Height));

        /// <summary>
        /// 取本尺寸与另一尺寸中较大的分量(用于布局联合)。
        /// </summary>
        public Size Expand(Size other) =>
            new Size(Math.Max(Width, other.Width), Math.Max(Height, other.Height));

        /// <summary>
        /// 解构,便于 tuple 解构语法。
        /// </summary>
        public void Deconstruct(out double width, out double height)
        {
            width = Width;
            height = Height;
        }
    }
}
