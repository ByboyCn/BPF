using Bpf.Controls.Routing;

namespace Bpf.Samples.HelloWorld;

/// <summary>
/// MainForm 的 code-behind。事件处理通过 ViewModel 间接驱动 UI:
/// 事件改 ViewModel 属性 → INPC 通知 → {Binding} 自动刷新 UI(无需手工改控件属性)。
/// 这就是数据绑定相比纯事件驱动的优势:UI 状态集中在 ViewModel。
/// </summary>
public partial class MainForm
{
    // ViewModel 实例由 Program.cs 创建并设为根控件的 DataContext;
    // 这里通过 it 访问。为简化,M6 用静态字段持有引用。
    internal static DemoViewModel? ViewModel { get; set; }

    // 滑块变化:回写 ViewModel.Value(OneWay 绑定会自动刷新显示)
    private static void OnSliderChanged(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null && mySlider != null)
            ViewModel.Value = (int)mySlider.Value;
    }

    // 按钮 +1:改 ViewModel.Count,绑定自动刷新下方文字
    private static void OnIncrementClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
            ViewModel.Count++;
    }
}
