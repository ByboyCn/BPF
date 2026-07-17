using Bpf.Controls.Routing;

namespace Bpf.Samples.HelloWorld;

/// <summary>
/// MainForm 的 code-behind。事件处理 + 背景设置。
/// </summary>
public partial class MainForm
{
    // 当前窗口引用(由 Program.cs 设置,用于改背景)
    internal static Bpf.Controls.Window? Window { get; set; }
    private static bool _bgImageOn;

    // 按钮点击:切换文字
    private static int _clickCount;
    private static void OnDemoButtonClick(object sender, RoutedEventArgs e)
    {
        _clickCount++;
        if (demoButton != null)
            demoButton.Content = _clickCount % 2 == 0 ? "点击我" : $"已点击 {_clickCount} 次";
    }

    // 滑块联动进度条
    private static void OnSliderChanged(object sender, RoutedEventArgs e)
    {
        if (demoProgress != null && demoSlider != null)
            demoProgress.Value = demoSlider.Value;
    }

    // 切换背景图片
    private static void OnBgImageClick(object sender, RoutedEventArgs e)
    {
        if (Window == null) return;
        _bgImageOn = !_bgImageOn;
        Window.BackgroundImage = _bgImageOn ? "logo.png" : null;
        if (bgImageButton != null)
            bgImageButton.Content = _bgImageOn ? "关闭背景图片" : "切换背景图片";
    }
}
