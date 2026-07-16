using Bpf.Data;

namespace Bpf.Samples.HelloWorld;

/// <summary>
/// M6 阶段 D 的演示 ViewModel。实现 INotifyPropertyChanged,
/// {Binding Xxx} 的源生成器会生成 vm => vm.Xxx 的强类型 lambda,
/// 当 Xxx 变化时(经 INPC)自动刷新绑定的目标控件。
/// </summary>
public sealed class DemoViewModel : INotifyPropertyChanged
{
    private int _value = 50;
    private int _count;
    private string _message = "这是 {Binding Message} 绑定的文字";

    /// <summary>滑块对应的值。</summary>
    public int Value
    {
        get => _value;
        set
        {
            if (this.Set(ref _value, value, nameof(Value), PropertyChanged))
            {
                // 派生属性 DisplayText 依赖 Value,需手动通知
                this.RaisePropertyChanged(nameof(DisplayText), PropertyChanged);
            }
        }
    }

    /// <summary>计数值。</summary>
    public int Count
    {
        get => _count;
        set
        {
            if (this.Set(ref _count, value, nameof(Count), PropertyChanged))
            {
                this.RaisePropertyChanged(nameof(CountText), PropertyChanged);
            }
        }
    }

    /// <summary>消息字符串(绑到 TextBlock.Text)。</summary>
    public string Message
    {
        get => _message;
        set { this.Set(ref _message, value, nameof(Message), PropertyChanged); }
    }

    /// <summary>派生属性:把 Value 格式化成字符串,绑到 UI 显示。</summary>
    public string DisplayText => $"滑块值 = {Value}";

    /// <summary>派生属性:把 Count 格式化。</summary>
    public string CountText => $"已点击 {Count} 次";

    public event PropertyChangedEventHandler? PropertyChanged;
}
