using Bpf.Media;

namespace Bpf.Theming
{
    /// <summary>
    /// 主题令牌:集中所有颜色/尺寸定义。控件 Render 和 Style 从此读取,实现统一样式。
    /// 用法:<c>Theme.Current = Theme.Dark;</c> 切换主题,然后 Theme.ApplyTo(window) 重应用。
    /// </summary>
    public sealed class Theme
    {
        // 静态构造:保证 Light/Dark 先构造,再赋给 Current(避免静态字段初始化顺序问题)
        static Theme()
        {
            s_current = Light;
        }
        // ── 颜色令牌 ──

        /// <summary>主强调色(按钮/选中/链接)。默认蓝。</summary>
        public Color Accent { get; set; }
        /// <summary>悬停态强调色(略深于 Accent)。</summary>
        public Color AccentHover { get; set; }
        /// <summary>按下态强调色(更深)。</summary>
        public Color AccentPressed { get; set; }

        /// <summary>普通文字前景色。</summary>
        public Color Foreground { get; set; }
        /// <summary>次要/灰色文字。</summary>
        public Color MutedForeground { get; set; }

        /// <summary>控件背景色(Button/TextBox/ComboBox 主体)。</summary>
        public Color Background { get; set; }
        /// <summary>悬停背景色(列表项/按钮悬停)。</summary>
        public Color HoverBackground { get; set; }
        /// <summary>按下背景色(Button)。</summary>
        public Color PressedBackground { get; set; }

        /// <summary>边框色。</summary>
        public Color Border { get; set; }
        /// <summary>聚焦边框色(略带强调)。</summary>
        public Color FocusBorder { get; set; }

        /// <summary>选区高亮色(TextBox 选区、ListBox/ComboBox 选中项)。</summary>
        public Color Selection { get; set; }
        /// <summary>选中项的文字色(在 Selection 背景上)。</summary>
        public Color SelectionForeground { get; set; }

        /// <summary>窗口/面板背景色。</summary>
        public Color WindowBackground { get; set; }

        /// <summary>滚动条轨道色。</summary>
        public Color ScrollBarTrack { get; set; }
        /// <summary>滚动条滑块色。</summary>
        public Color ScrollBarThumb { get; set; }

        // ── 尺寸令牌 ──

        /// <summary>圆角半径(Button/滑块等)。</summary>
        public double CornerRadius { get; set; } = 3;
        /// <summary>边框粗细。</summary>
        public double BorderThickness { get; set; } = 1.0;
        /// <summary>默认字号。</summary>
        public double FontSize { get; set; } = 14.0;
        /// <summary>默认字体族。</summary>
        public string FontFamily { get; set; } = "Segoe UI";
        /// <summary>列表项高度。</summary>
        public double ItemHeight { get; set; } = 24;
        /// <summary>滚动条宽度。</summary>
        public double ScrollBarWidth { get; set; } = 14;

        // ── 当前主题单例 ──
        // 用静态构造函数保证 Light 先于 Current 初始化(静态字段顺序问题)。
        private static Theme s_current;
        /// <summary>当前生效的主题。改此属性后需调用 ApplyTo 重应用。</summary>
        public static Theme Current
        {
            get => s_current;
            set => s_current = value;
        }

        // ── 预设 ──

        /// <summary>浅色主题(默认)。</summary>
        public static Theme Light { get; } = new Theme
        {
            Accent = Color.FromRgb(0x2D, 0x7F, 0xF9),
            AccentHover = Color.FromRgb(0x1A, 0x66, 0xD6),
            AccentPressed = Color.FromRgb(0x14, 0x52, 0xA8),
            Foreground = Color.FromRgb(0x20, 0x20, 0x20),
            MutedForeground = Color.FromRgb(0x88, 0x88, 0x88),
            Background = Color.FromRgb(0xDD, 0xDD, 0xDD),  // Button 默认浅灰
            HoverBackground = Color.FromRgb(0xC8, 0xC8, 0xC8),
            PressedBackground = Color.FromRgb(0xB0, 0xB0, 0xB0),
            Border = Color.FromRgb(0x88, 0x88, 0x88),
            FocusBorder = Color.FromRgb(0x2D, 0x7F, 0xF9),
            Selection = Color.FromRgb(0x2D, 0x8C, 0xFF),
            SelectionForeground = Color.White,
            WindowBackground = Color.White,
            ScrollBarTrack = Color.FromRgb(0xF0, 0xF0, 0xF0),
            ScrollBarThumb = Color.FromRgb(0xC0, 0xC0, 0xC0),
        };

        /// <summary>深色主题。</summary>
        public static Theme Dark { get; } = new Theme
        {
            Accent = Color.FromRgb(0x4A, 0x9C, 0xFF),
            AccentHover = Color.FromRgb(0x66, 0xAE, 0xFF),
            AccentPressed = Color.FromRgb(0x33, 0x7A, 0xCC),
            Foreground = Color.FromRgb(0xE8, 0xE8, 0xE8),
            MutedForeground = Color.FromRgb(0x99, 0x99, 0x99),
            Background = Color.FromRgb(0x3A, 0x3A, 0x3A),  // Button 深灰
            HoverBackground = Color.FromRgb(0x4A, 0x4A, 0x4A),
            PressedBackground = Color.FromRgb(0x2A, 0x2A, 0x2A),
            Border = Color.FromRgb(0x55, 0x55, 0x55),
            FocusBorder = Color.FromRgb(0x4A, 0x9C, 0xFF),
            Selection = Color.FromRgb(0x2D, 0x6D, 0xD6),
            SelectionForeground = Color.White,
            WindowBackground = Color.FromRgb(0x24, 0x24, 0x24),
            ScrollBarTrack = Color.FromRgb(0x2A, 0x2A, 0x2A),
            ScrollBarThumb = Color.FromRgb(0x60, 0x60, 0x60),
        };
    }
}
