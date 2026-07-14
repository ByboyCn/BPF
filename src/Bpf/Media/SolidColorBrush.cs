using Bpf.Platform;

namespace Bpf.Media
{
    /// <summary>
    /// 纯色画笔。
    /// </summary>
    public sealed class SolidColorBrush : Brush
    {
        public Color Color { get; set; } = Colors.Black;

        public SolidColorBrush() { }

        public SolidColorBrush(Color color)
        {
            Color = color;
        }

        public SolidColorBrush(uint rgb) : this(Color.FromUInt32(rgb)) { }

        internal override IPlatformBrush ToPlatform(IPlatformRenderInterface render)
            => render.CreateSolidColorBrush(Color);
    }

    /// <summary>常用颜色快捷访问(Avalonia 风格)。</summary>
    public static class Colors
    {
        public static Color Transparent => Color.Transparent;
        public static Color Black => Color.Black;
        public static Color White => Color.White;
        public static Color Red => Color.Red;
        public static Color Green => Color.Green;
        public static Color Blue => Color.Blue;
        public static Color Gray => Color.Gray;
        public static Color LightGray => Color.LightGray;
        public static Color DarkGray => Color.DarkGray;
    }
}
