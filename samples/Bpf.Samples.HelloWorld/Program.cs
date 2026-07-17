using System;
using Bpf;
using Bpf.Controls;
using Bpf.Controls.Routing;
using Bpf.Data;
using Bpf.Media;

namespace Bpf.Samples.HelloWorld;

internal static class Program
{
    private static void Main()
    {
        try { RunBpfamlApp(); }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"[bpf FATAL] {ex}");
            System.Console.Error.Flush();
            throw;
        }
    }

    /// <summary>M6/M7/M10:.bpfaml 声明式 UI + 默认主题库演示。</summary>
    private static void RunBpfamlApp()
    {
        var app = Bpf.Windows.WindowsAppExtensions.UseWindows();
        var window = app.CreateWindow(520, 920);
        window.Title = "bpf M10:默认主题库(亮/暗切换)";

        // MainForm.Build() 由源生成器从 MainForm.bpfaml 生成,
        // 构造并返回 .bpfaml 描述的控件树根(StackPanel)。
        var form = MainForm.Build();

        // M10:应用默认浅色主题(覆盖各控件属性默认值)
        Bpf.Theming.ThemeApplier.Apply(form);

        // 设置 DataContext:ViewModel。子控件通过 {Binding} 沿树继承获取源。
        var vm = new DemoViewModel();
        MainForm.ViewModel = vm;
        MainForm.Root = form;  // M10:供主题切换按钮重应用
        form.DataContext = vm;

        // M7:给 ListBox 填充测试数据
        if (MainForm.myList != null)
        {
            var items = new ObservableCollection<string>();
            for (int i = 1; i <= 20; i++)
                items.Add($"列表项 {i:D2} —— 悬停看高亮,滚动看 ScrollViewer");
            MainForm.myList.ItemsSource = items;
        }

        // M12:给 TreeView 填充树形数据(文件资源管理器风格)
        if (MainForm.treeView != null)
        {
            var root = new Bpf.Controls.TreeNode { Header = "项目" };
            var src = root.AddChild("src");
            src.AddChild("Bpf.cs");
            src.AddChild("Program.cs");
            var docs = root.AddChild("docs");
            docs.AddChild("README.md");
            docs.AddChild("API.md");
            root.AddChild("README.md");
            root.Children[0].IsExpanded = false; // src 默认收起
            MainForm.treeView.AddRoot(root);
        }

        // M13:给 Menu 填充菜单项
        if (MainForm.menuBar != null)
        {
            var file = MainForm.menuBar.AddMenu("文件");
            file.AddItem("新建").Click += (s, e) => System.Console.WriteLine("[Menu] 新建");
            file.AddItem("打开").Click += (s, e) => System.Console.WriteLine("[Menu] 打开");
            file.AddItem("保存").Click += (s, e) => System.Console.WriteLine("[Menu] 保存");
            var edit = MainForm.menuBar.AddMenu("编辑");
            edit.AddItem("撤销").Click += (s, e) => System.Console.WriteLine("[Menu] 撤销");
            edit.AddItem("重做").Click += (s, e) => System.Console.WriteLine("[Menu] 重做");
            var help = MainForm.menuBar.AddMenu("帮助");
            help.AddItem("关于").Click += (s, e) => System.Console.WriteLine("[Menu] 关于");
        }

        // M13:给 DataGrid 填充数据
        if (MainForm.dataGrid != null)
        {
            MainForm.dataGrid.AddColumn("姓名", 100);
            MainForm.dataGrid.AddColumn("年龄", 60);
            MainForm.dataGrid.AddColumn("城市", 0);
            MainForm.dataGrid.SetRows(new[]
            {
                new[] { "张三", "25", "北京" },
                new[] { "李四", "30", "上海" },
                new[] { "王五", "28", "广州" },
                new[] { "赵六", "35", "深圳" },
                new[] { "钱七", "22", "杭州" },
            });
        }

        window.SetContent(form);

        app.Run();
    }

    private static void RunApp()
    {
        var app = Bpf.Windows.WindowsAppExtensions.UseWindows();
        var window = app.CreateWindow(600, 500);
        window.Title = "bpf M4.1: ScrollViewer + ComboBox + Converter";

        // ── 数据:15 个人(验证 ScrollViewer 滚动) ──
        var people = new ObservableCollection<Person>();
        for (int i = 0; i < 15; i++)
            people.Add(new Person { Name = $"用户 {i + 1}", Age = 20 + i });

        // ── 布局:Grid 两列 ──
        var grid = new Grid();
        grid.Columns = "200,*";
        grid.Rows = "*";

        // 左列:ScrollViewer 包裹 ListBox(15 项,超出可滚动)
        var scrollViewer = new ScrollViewer();
        Grid.SetColumn(scrollViewer, 0);
        grid.AddChild(scrollViewer);

        var listBox = new ListBox { ItemsSource = people, FontSize = 14 };
        scrollViewer.Child = listBox;

        // 右列:控件演示面板
        var rightPanel = new StackPanel { Orientation = Orientation.Vertical };
        Grid.SetColumn(rightPanel, 1);
        grid.AddChild(rightPanel);

        // 标题
        rightPanel.AddChild(new Label { Text = "M4.1 新功能演示", FontSize = 16 });

        // Image(SkiaSharp 加载 PNG 位图)
        rightPanel.AddChild(new TextBlock { Text = "Image (PNG):", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)) });
        rightPanel.AddChild(new Image { Source = "logo.png", Stretch = Stretch.Fill });

        // Image(SVG 矢量图,自研光栅化器)
        rightPanel.AddChild(new TextBlock { Text = "Image (SVG):", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)) });
        rightPanel.AddChild(new Image { Source = "logo.svg", Stretch = Stretch.Uniform });

        // ComboBox(选择城市)
        var cities = new[] { "北京", "上海", "广州", "深圳", "杭州" };
        rightPanel.AddChild(new TextBlock { Text = "ComboBox:", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)) });
        var comboBox = new ComboBox
        {
            ItemsSource = cities,
            FontSize = 14,
        };
        rightPanel.AddChild(comboBox);

        // 选中人信息显示(用 Converter 演示)
        rightPanel.AddChild(new TextBlock { Text = "选中信息:", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)) });

        var nameBox = new TextBox { Text = "(选择一个人)" };
        rightPanel.AddChild(nameBox);

        var ageSlider = new Slider { Minimum = 0, Maximum = 100, Value = 0 };
        rightPanel.AddChild(ageSlider);

        // Converter 演示:Slider 的 double 值 → 整数字符串
        var ageLabel = new Label { Text = "年龄: 0", FontSize = 14 };
        rightPanel.AddChild(ageLabel);

        var cityLabel = new Label { Text = "城市: -", FontSize = 14 };
        rightPanel.AddChild(cityLabel);

        // ── 事件同步 ──
        listBox.SelectionChanged += (s, e) =>
        {
            if (listBox.SelectedItem is Person p)
            {
                nameBox.Text = p.Name;
                ageSlider.Value = p.Age;
                ageLabel.Text = $"年龄: {p.Age}";
            }
        };

        // TextBox 改 → 回写 Person.Name
        nameBox.TextChanged += (s, e) =>
        {
            if (listBox.SelectedItem is Person p)
            {
                p.Name = nameBox.Text;
            }
        };

        // Slider 改 → 回写 Person.Age + Converter 演示(手动格式化)
        ageSlider.ValueChanged += (s, e) =>
        {
            if (listBox.SelectedItem is Person p)
            {
                p.Age = (int)ageSlider.Value;
                // Converter 演示:用 DoubleToStringConverter 把 double 格式化
                var converter = new DoubleToStringConverter("F0");
                var displayAge = converter.Convert(ageSlider.Value, typeof(string), null)?.ToString();
                ageLabel.Text = $"年龄: {displayAge}";
            }
        };

        // ComboBox 选择
        comboBox.SelectionChanged += (s, e) =>
        {
            cityLabel.Text = $"城市: {comboBox.SelectedItem}";
        };

        // 默认选中
        listBox.SelectedIndex = 0;

        window.SetContent(grid);
        app.Run();
    }
}

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
