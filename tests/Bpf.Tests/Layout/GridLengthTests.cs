using System;
using Bpf.Layout;
using Xunit;

namespace Bpf.Tests.Layout
{
    public class GridLengthTests
    {
        [Fact]
        public void Parse_Auto()
        {
            var g = GridLength.Parse("auto");
            Assert.True(g.IsAuto);
            Assert.Equal(0, g.Value);
        }

        [Theory]
        [InlineData("AUTO")]
        [InlineData("Auto")]
        [InlineData("  auto  ")]  // 空白 + 大小写
        public void Parse_Auto_CaseInsensitive(string token)
        {
            Assert.True(GridLength.Parse(token).IsAuto);
        }

        [Fact]
        public void Parse_Star_Default()
        {
            var g = GridLength.Parse("*");
            Assert.True(g.IsStar);
            Assert.Equal(1, g.Value);
        }

        [Theory]
        [InlineData("2*", 2)]
        [InlineData("1.5*", 1.5)]
        [InlineData("0.5*", 0.5)]
        public void Parse_Star_WithCoefficient(string token, double expectedValue)
        {
            var g = GridLength.Parse(token);
            Assert.True(g.IsStar);
            Assert.Equal(expectedValue, g.Value);
        }

        [Theory]
        [InlineData("100", 100)]
        [InlineData("0", 0)]
        [InlineData("50.5", 50.5)]
        public void Parse_Pixel(string token, double expectedValue)
        {
            var g = GridLength.Parse(token);
            Assert.True(g.IsAbsolute);
            Assert.Equal(expectedValue, g.Value);
        }

        [Fact]
        public void Parse_Empty_ReturnsAuto()
        {
            Assert.True(GridLength.Parse("").IsAuto);
            Assert.True(GridLength.Parse("   ").IsAuto);
        }

        [Fact]
        public void ParseAll_MultipleDefinitions()
        {
            var arr = GridLength.ParseAll("auto,*,100,2*");
            Assert.Equal(4, arr.Length);
            Assert.True(arr[0].IsAuto);
            Assert.True(arr[1].IsStar);
            Assert.Equal(1, arr[1].Value);
            Assert.True(arr[2].IsAbsolute);
            Assert.Equal(100, arr[2].Value);
            Assert.True(arr[3].IsStar);
            Assert.Equal(2, arr[3].Value);
        }

        [Fact]
        public void ParseAll_Empty_ReturnsEmptyArray()
        {
            Assert.Empty(GridLength.ParseAll(""));
            Assert.Empty(GridLength.ParseAll(null!));
        }

        [Fact]
        public void Equality()
        {
            Assert.Equal(new GridLength(100), new GridLength(100));
            Assert.NotEqual(new GridLength(100), new GridLength(200));
            Assert.True(new GridLength(1, GridUnitType.Star) == GridLength.Star);
            Assert.True(new GridLength(100) != GridLength.Star); // 不同单位
        }

        [Fact]
        public void Constructor_RejectsNegative()
        {
            Assert.Throws<ArgumentException>(() => new GridLength(-1));
            Assert.Throws<ArgumentException>(() => new GridLength(double.NaN));
            Assert.Throws<ArgumentException>(() => new GridLength(double.PositiveInfinity));
        }

        [Theory]
        [InlineData(GridUnitType.Auto, "Auto")]
        [InlineData(GridUnitType.Star, "*")]           // value=1
        public void ToString_Formats(GridUnitType unit, string expected)
        {
            var g = unit == GridUnitType.Auto ? GridLength.Auto : new GridLength(1, unit);
            Assert.Equal(expected, g.ToString());
        }

        [Fact]
        public void ToString_CoefficientStar()
        {
            Assert.Equal("2*", new GridLength(2, GridUnitType.Star).ToString());
        }
    }
}
