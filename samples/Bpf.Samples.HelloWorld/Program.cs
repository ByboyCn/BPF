using System;
using Bpf;
using Bpf.Controls;
using Bpf.Controls.Routing;
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
        var window = app.CreateWindow(560, 480);
        window.Title = "bpf 控件演示 (M3)";

        // ── 全局样式:统一 Button 配色 ──
        window.Styles.Add(new Style<Button>()
            .Add(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x2D, 0x8C, 0xFF)))
            .Add(Button.ForegroundProperty, new SolidColorBrush(Color.White))
            .Add(Button.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0x1A, 0x5C, 0xCC))));

        // ── 布局:Grid 两列 ──
        var grid = new Grid();
        grid.Columns = "180,*";
        grid.Rows = "*";

        // 左列:控件面板(StackPanel)
        var leftPanel = new StackPanel { Orientation = Orientation.Vertical };
        Grid.SetColumn(leftPanel, 0);
        grid.AddChild(leftPanel);

        // 右列:显示区
        var rightPanel = new StackPanel { Orientation = Orientation.Vertical };
        Grid.SetColumn(rightPanel, 1);
        grid.AddChild(rightPanel);

        // ── 左列控件 ──
        leftPanel.AddChild(new TextBlock { Text = "控件演示", FontSize = 16, Foreground = new SolidColorBrush(Color.Black) });

        // TextBox
        var textBox = new TextBox { Text = "点击输入..." };
        leftPanel.AddChild(textBox);

        // CheckBox
        var checkBox = new CheckBox { Content = "启用某功能" };
        leftPanel.AddChild(checkBox);

        // RadioButton 组
        var rb1 = new RadioButton { Content = "选项 A", GroupName = "g1" };
        var rb2 = new RadioButton { Content = "选项 B", GroupName = "g1" };
        var rb3 = new RadioButton { Content = "选项 C", GroupName = "g1" };
        rb1.IsChecked = true;
        leftPanel.AddChild(rb1);
        leftPanel.AddChild(rb2);
        leftPanel.AddChild(rb3);

        // Slider
        var slider = new Slider { Minimum = 0, Maximum = 100, Value = 50 };
        leftPanel.AddChild(slider);

        // Button(受全局样式影响)
        var btn = new Button { Content = "应用" };
        leftPanel.AddChild(btn);

        // ── 右列:状态显示 ──
        rightPanel.AddChild(new TextBlock { Text = "当前状态", FontSize = 16, Foreground = new SolidColorBrush(Color.Black) });

        var statusText = new TextBlock
        {
            Text = "TextBox: 点击输入...\nCheckBox: 未勾选\nRadioButton: 选项 A\nSlider: 50",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
        };
        rightPanel.AddChild(statusText);

        // ── 事件:更新状态 ──
        textBox.KeyDown += (s, e) => { UpdateStatus(); };
        checkBox.Checked += (s, e) => UpdateStatus();
        checkBox.Unchecked += (s, e) => UpdateStatus();
        rb1.Checked += (s, e) => UpdateStatus();
        rb2.Checked += (s, e) => UpdateStatus();
        rb3.Checked += (s, e) => UpdateStatus();
        slider.ValueChanged += (s, e) => UpdateStatus();
        btn.Click += (s, e) => { statusText.Text = "已点击[应用]!"; };

        void UpdateStatus()
        {
            string rb = rb1.IsChecked ? "A" : rb2.IsChecked ? "B" : rb3.IsChecked ? "C" : "无";
            statusText.Text =
                $"TextBox: {textBox.Text}\n" +
                $"CheckBox: {(checkBox.IsChecked ? "已勾选" : "未勾选")}\n" +
                $"RadioButton: 选项 {rb}\n" +
                $"Slider: {slider.Value:F0}";
        }

        window.SetContent(grid);
        app.Run();
    }
}
