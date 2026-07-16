using Bpf.Controls.Routing;

namespace Bpf.Samples.HelloWorld;

/// <summary>
/// MainForm 的 code-behind。事件处理通过 ViewModel 间接驱动 UI。
/// </summary>
public partial class MainForm
{
    // ViewModel 实例由 Program.cs 创建并设为根控件的 DataContext。
    internal static DemoViewModel? ViewModel { get; set; }

    // M10:主题切换需要重应用,持有根控件引用(Program.cs 设置)
    internal static Bpf.Controls.Control? Root { get; set; }

    private static void OnSliderChanged(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null && mySlider != null)
            ViewModel.Value = (int)mySlider.Value;
    }

    private static void OnIncrementClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
            ViewModel.Count++;
    }

    // M10:亮/暗主题切换
    private static void OnThemeToggle(object sender, RoutedEventArgs e)
    {
        if (themeButton == null || Root == null) return;
        bool toDark = Bpf.Theming.Theme.Current == Bpf.Theming.Theme.Light;
        Bpf.Theming.Theme.Current = toDark ? Bpf.Theming.Theme.Dark : Bpf.Theming.Theme.Light;
        Bpf.Theming.ThemeApplier.Apply(Root);
        themeButton.Content = toDark ? "切换到亮色主题" : "切换到暗色主题";
    }
}
