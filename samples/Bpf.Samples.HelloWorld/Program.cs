using System;
using Bpf;
using Bpf.Controls;
using Bpf.Controls.Routing;
using Bpf.Data;
using Bpf.Media;
using Bpf.Styling;

namespace Bpf.Samples.HelloWorld;

internal static class Program
{
    private static void Main()
    {
        try { RunApp(); }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"[bpf FATAL] {ex}");
            System.Console.Error.Flush();
            throw;
        }
    }

    private static void RunApp()
    {
        var app = Bpf.Windows.WindowsAppExtensions.UseWindows();
        var window = app.CreateWindow(600, 440);
        window.Title = "bpf 数据绑定 + ListBox (M4)";

        // ── 数据:Person 列表 ──
        var people = new ObservableCollection<Person>
        {
            new Person { Name = "Alice", Age = 28 },
            new Person { Name = "Bob", Age = 35 },
            new Person { Name = "Charlie", Age = 22 },
            new Person { Name = "Diana", Age = 40 },
        };

        // ── 布局:Grid 两列 ──
        var grid = new Grid();
        grid.Columns = "200,*";
        grid.Rows = "*";

        // 左列:ListBox
        var listBox = new ListBox
        {
            ItemsSource = people,
            FontSize = 14,
        };
        Grid.SetColumn(listBox, 0);
        grid.AddChild(listBox);

        // 右列:编辑面板
        var rightPanel = new StackPanel { Orientation = Orientation.Vertical };
        Grid.SetColumn(rightPanel, 1);
        grid.AddChild(rightPanel);

        rightPanel.AddChild(new TextBlock { Text = "编辑选中项", FontSize = 16, Foreground = new SolidColorBrush(Color.Black) });

        // TextBox 绑定 Name(OneWay,手动同步)
        var nameBox = new TextBox { Text = "(选择一个人)" };
        rightPanel.AddChild(nameBox);

        // Slider 绑定 Age
        var ageSlider = new Slider { Minimum = 0, Maximum = 100, Value = 0 };
        rightPanel.AddChild(ageSlider);

        var ageLabel = new TextBlock { Text = "年龄: 0", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)) };
        rightPanel.AddChild(ageLabel);

        var nameLabel = new TextBlock { Text = "姓名: -", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)) };
        rightPanel.AddChild(nameLabel);

        // ── 同步逻辑(ListBox 选择 → 编辑框) ──
        listBox.SelectionChanged += (s, e) =>
        {
            var selected = listBox.SelectedItem as Person;
            if (selected is null)
            {
                nameBox.Text = "(无选中)";
                ageSlider.Value = 0;
                nameLabel.Text = "姓名: -";
                ageLabel.Text = "年龄: -";
            }
            else
            {
                nameBox.Text = selected.Name;
                ageSlider.Value = selected.Age;
                nameLabel.Text = $"姓名: {selected.Name}";
                ageLabel.Text = $"年龄: {selected.Age}";
            }
        };

        // TextBox 改 → 回写 Person.Name(ListBox 自动监听 PropertyChanged 刷新)
        nameBox.TextChanged += (s, e) =>
        {
            if (listBox.SelectedItem is Person p)
            {
                p.Name = nameBox.Text;
                nameLabel.Text = $"姓名: {p.Name}";
            }
        };

        // Slider 改 → 回写 Person.Age(ListBox 自动刷新)
        ageSlider.ValueChanged += (s, e) =>
        {
            ageLabel.Text = $"年龄: {ageSlider.Value:F0}";
            if (listBox.SelectedItem is Person p)
            {
                p.Age = (int)ageSlider.Value;
            }
        };

        // 默认选中第一个
        listBox.SelectedIndex = 0;

        window.SetContent(grid);
        app.Run();
    }
}

/// <summary>
/// 数据模型:实现 INotifyPropertyChanged,Name/Age 变化时通知绑定。
/// </summary>
public sealed class Person : INotifyPropertyChanged
{
    private string _name = "";
    private int _age;

    public string Name
    {
        get => _name;
        set => this.Set(ref _name, value, nameof(Name), PropertyChanged);
    }

    public int Age
    {
        get => _age;
        set => this.Set(ref _age, value, nameof(Age), PropertyChanged);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString() => $"{_name} ({_age})";
}
