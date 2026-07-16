using Bpf.Media;
using Xunit;

namespace Bpf.Tests.Media
{
    public class ColorTests
    {
        [Theory]
        [InlineData("#FFF", 0xFF, 0xFF, 0xFF, 0xFF)]       // #RGB → 扩展,不透明
        [InlineData("#000", 0xFF, 0x00, 0x00, 0x00)]
        [InlineData("#F00", 0xFF, 0xFF, 0x00, 0x00)]        // 红
        [InlineData("#FF8800", 0xFF, 0xFF, 0x88, 0x00)]     // #RRGGBB
        [InlineData("#2D7FF9", 0xFF, 0x2D, 0x7F, 0xF9)]
        [InlineData("#80FF0000", 0x80, 0xFF, 0x00, 0x00)]   // #AARRGGBB 半透明红
        public void Parse_HexFormats(string input, byte a, byte r, byte g, byte b)
        {
            var c = Color.Parse(input);
            Assert.Equal(a, c.A);
            Assert.Equal(r, c.R);
            Assert.Equal(g, c.G);
            Assert.Equal(b, c.B);
        }

        [Theory]
        [InlineData("Red", 255, 0, 0)]
        [InlineData("red", 255, 0, 0)]       // 大小写不敏感
        [InlineData("RED", 255, 0, 0)]
        [InlineData("White", 255, 255, 255)]
        [InlineData("Black", 0, 0, 0)]
        [InlineData("Green", 0, 128, 0)]
        [InlineData("Blue", 0, 0, 255)]
        [InlineData("gray", 128, 128, 128)]
        [InlineData("grey", 128, 128, 128)]  // gray/grey 都支持
        [InlineData("Transparent", 0, 0, 0)] // alpha=0
        public void Parse_NamedColors(string name, byte r, byte g, byte b)
        {
            var c = Color.Parse(name);
            Assert.Equal(r, c.R);
            Assert.Equal(g, c.G);
            Assert.Equal(b, c.B);
        }

        [Fact]
        public void Parse_Transparent_HasZeroAlpha()
        {
            var c = Color.Parse("Transparent");
            Assert.Equal(0, c.A);
        }

        [Fact]
        public void Parse_Invalid_Throws()
        {
            Assert.Throws<System.FormatException>(() => Color.Parse("#ZZZ"));
            Assert.Throws<System.FormatException>(() => Color.Parse("NotAColor"));
            Assert.Throws<System.FormatException>(() => Color.Parse("#12"));  // 长度不对
        }

        [Fact]
        public void TryParse_Valid_ReturnsTrue()
        {
            Assert.True(Color.TryParse("#FF8800", out var c));
            Assert.Equal(0xFF, c.R);
        }

        [Fact]
        public void TryParse_Invalid_ReturnsFalse()
        {
            Assert.False(Color.TryParse("xxx", out var c));
        }

        [Fact]
        public void FromRgb_HasFullAlpha()
        {
            var c = Color.FromRgb(10, 20, 30);
            Assert.Equal(255, c.A);
            Assert.Equal(10, c.R);
        }

        [Fact]
        public void Equality()
        {
            Assert.Equal(Color.FromRgb(1, 2, 3), Color.FromRgb(1, 2, 3));
            Assert.NotEqual(Color.FromRgb(1, 2, 3), Color.FromRgb(1, 2, 4));
            Assert.True(Color.FromRgb(1, 2, 3) == Color.FromRgb(1, 2, 3));
            Assert.True(Color.FromRgb(1, 2, 3) != Color.FromRgb(1, 2, 4));
        }

        [Fact]
        public void ToString_ProducesHex()
        {
            var c = Color.FromArgb(0xFF, 0x2D, 0x7F, 0xF9);
            Assert.Equal("#FF2D7FF9", c.ToString());
        }
    }
}
