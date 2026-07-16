using Bpf.Controls;
using Bpf.Media;
using Bpf.Styling;

namespace Bpf.Theming
{
    /// <summary>
    /// 主题应用器:把 Theme 令牌转换成一组 Style,加到根控件的 Styles(覆盖各控件属性默认值)。
    /// 应用后,根控件的所有后代在 GetValue 时会沿祖先链找到这些 Style,拿到主题化的值。
    ///
    /// 用法:Theme.Current = Theme.Dark; ThemeApplier.Apply(window);
    /// 切换主题:Theme.Current = Theme.Light; ThemeApplier.Apply(window);(先 Clear 再 Apply)
    /// </summary>
    public static class ThemeApplier
    {
        /// <summary>标记:已应用的主题样式数(用于切换时 Clear 识别)。</summary>
        private const string AppliedKey = "__bpf_theme_applied";

        /// <summary>把 Theme.Current 应用到根控件(及其后代)。先清除上次的主题样式。</summary>
        public static void Apply(Control root)
        {
            Apply(root, Theme.Current);
        }

        /// <summary>应用指定主题到根控件。可重复调用切换主题。</summary>
        public static void Apply(Control root, Theme theme)
        {
            Clear(root);

            Brush AccentBrush = Solid(theme.Accent);
            Brush ForegroundBrush = Solid(theme.Foreground);
            Brush BackgroundBrush = Solid(theme.Background);
            Brush BorderBrush = Solid(theme.Border);
            Brush SelectionBrush = Solid(theme.Selection);
            Brush SelectionFgBrush = Solid(theme.SelectionForeground);

            // Button
            root.Styles.Add(new Style<Button>()
                .Add(Button.BackgroundProperty, BackgroundBrush)
                .Add(Button.ForegroundProperty, ForegroundBrush)
                .Add(Button.BorderBrushProperty, BorderBrush)
                .Add(Button.FontSizeProperty, theme.FontSize)
                .Add(Button.FontFamilyProperty, theme.FontFamily));

            // TextBox
            root.Styles.Add(new Style<TextBox>()
                .Add(TextBox.ForegroundProperty, ForegroundBrush)
                .Add(TextBox.BackgroundProperty, Solid(theme.WindowBackground))
                .Add(TextBox.BorderBrushProperty, BorderBrush)
                .Add(TextBox.SelectionBrushProperty, SelectionBrush)
                .Add(TextBox.FontSizeProperty, theme.FontSize)
                .Add(TextBox.FontFamilyProperty, theme.FontFamily));

            // Label / TextBlock
            root.Styles.Add(new Style<Label>()
                .Add(Label.ForegroundProperty, ForegroundBrush)
                .Add(Label.FontSizeProperty, theme.FontSize)
                .Add(Label.FontFamilyProperty, theme.FontFamily));
            root.Styles.Add(new Style<TextBlock>()
                .Add(TextBlock.ForegroundProperty, ForegroundBrush)
                .Add(TextBlock.FontSizeProperty, theme.FontSize)
                .Add(TextBlock.FontFamilyProperty, theme.FontFamily));

            // CheckBox / RadioButton(强调色用于勾选/选中标记)
            root.Styles.Add(new Style<CheckBox>()
                .Add(CheckBox.ForegroundProperty, ForegroundBrush)
                .Add(CheckBox.CheckMarkBrushProperty, AccentBrush)
                .Add(CheckBox.FontSizeProperty, theme.FontSize)
                .Add(CheckBox.FontFamilyProperty, theme.FontFamily));
            root.Styles.Add(new Style<RadioButton>()
                .Add(RadioButton.ForegroundProperty, ForegroundBrush)
                .Add(RadioButton.AccentBrushProperty, AccentBrush)
                .Add(RadioButton.FontSizeProperty, theme.FontSize)
                .Add(RadioButton.FontFamilyProperty, theme.FontFamily));

            // ListBox / ComboBox(选中色)
            root.Styles.Add(new Style<ListBox>()
                .Add(ListBox.ForegroundProperty, ForegroundBrush)
                .Add(ListBox.SelectedBackgroundProperty, SelectionBrush)
                .Add(ListBox.SelectedForegroundProperty, SelectionFgBrush)
                .Add(ListBox.FontSizeProperty, theme.FontSize)
                .Add(ListBox.FontFamilyProperty, theme.FontFamily));
            root.Styles.Add(new Style<ComboBox>()
                .Add(ComboBox.BackgroundProperty, Solid(theme.WindowBackground))
                .Add(ComboBox.BorderBrushProperty, BorderBrush)
                .Add(ComboBox.SelectedBackgroundProperty, SelectionBrush)
                .Add(ComboBox.FontSizeProperty, theme.FontSize)
                .Add(ComboBox.FontFamilyProperty, theme.FontFamily));

            // Slider(轨道 + 滑块用强调色)
            root.Styles.Add(new Style<Slider>()
                .Add(Slider.TrackBrushProperty, Solid(theme.ScrollBarThumb))
                .Add(Slider.ThumbBrushProperty, AccentBrush));

            // Border
            root.Styles.Add(new Style<Border>()
                .Add(Border.BorderBrushProperty, BorderBrush));

            // 触发重绘以反映新主题(InvalidateArrange 是 public,会连锁触发重排+重绘)
            root.InvalidateArrange();
        }

        /// <summary>清除上次应用的主题样式(通过对比数量回退)。</summary>
        public static void Clear(Control root)
        {
            // 简化:清除所有 Style。用户自定义 Style 需在 Apply 后重新加。
            // 后续可用 Setter 元数据标记主题样式,精确清除。
            root.Styles.Clear();
        }

        private static SolidColorBrush Solid(Color c) => new SolidColorBrush(c);
    }
}
