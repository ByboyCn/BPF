using Bpf;
using Xunit;

namespace Bpf.Tests
{
    public class GeometryTests
    {
        // ── Rect ──

        [Fact]
        public void Rect_Contains_Point()
        {
            var r = new Rect(10, 20, 100, 50);
            Assert.True(r.Contains(new Point(10, 20)));   // 左上角(含边界)
            Assert.True(r.Contains(new Point(50, 45)));   // 内部
            Assert.True(r.Contains(new Point(109, 69)));  // 右下角内
            Assert.False(r.Contains(new Point(0, 0)));    // 外部
            Assert.False(r.Contains(new Point(200, 200)));
        }

        [Fact]
        public void Rect_Contains_Rect()
        {
            var outer = new Rect(0, 0, 100, 100);
            Assert.True(outer.Contains(new Rect(10, 10, 50, 50)));
            Assert.False(outer.Contains(new Rect(90, 90, 50, 50))); // 部分超出
        }

        [Fact]
        public void Rect_Deflate_Thickness()
        {
            var r = new Rect(0, 0, 100, 100);
            var deflated = r.Deflate(new Thickness(10, 20, 10, 20));
            Assert.Equal(new Rect(10, 20, 80, 60), deflated);
        }

        [Fact]
        public void Rect_Inflate_Thickness()
        {
            var r = new Rect(10, 10, 80, 80);
            var inflated = r.Inflate(new Thickness(10, 10, 10, 10));
            Assert.Equal(new Rect(0, 0, 100, 100), inflated);
        }

        [Fact]
        public void Rect_Offset_Operator()
        {
            var r = new Rect(0, 0, 10, 10);
            var moved = r + new Vector(5, 7);
            Assert.Equal(new Rect(5, 7, 10, 10), moved);
        }

        // ── Size ──

        [Fact]
        public void Size_PlusMinus_Thickness()
        {
            var s = new Size(100, 100);
            Assert.Equal(new Size(120, 140), s + new Thickness(10, 20, 10, 20));
            Assert.Equal(new Size(80, 60), s - new Thickness(10, 20, 10, 20));
        }

        [Fact]
        public void Size_Infinity()
        {
            var inf = Size.Infinity;
            Assert.Equal(double.PositiveInfinity, inf.Width);
            Assert.Equal(double.PositiveInfinity, inf.Height);
        }

        [Fact]
        public void Size_CastToVector()
        {
            var s = new Size(30, 40);
            var v = (Vector)s;
            Assert.Equal(30, v.X);
            Assert.Equal(40, v.Y);
        }

        // ── Thickness ──

        [Fact]
        public void Thickness_Constructors()
        {
            Assert.Equal(new Thickness(5, 5, 5, 5), new Thickness(5));        // 均匀
            Assert.Equal(new Thickness(3, 5, 3, 5), new Thickness(3, 5));     // 水平+垂直
            Assert.Equal(new Thickness(1, 2, 3, 4), new Thickness(1, 2, 3, 4)); // 四向
        }

        [Fact]
        public void Thickness_Addition()
        {
            var a = new Thickness(1, 2, 3, 4);
            var b = new Thickness(10, 20, 30, 40);
            Assert.Equal(new Thickness(11, 22, 33, 44), a + b);
        }

        // ── Point / Vector ──

        [Fact]
        public void Vector_Negation()
        {
            var v = new Vector(3, -5);
            var neg = -v;
            Assert.Equal(-3, neg.X);
            Assert.Equal(5, neg.Y);
        }
    }
}
