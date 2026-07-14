using System;
using Bpf;
using Bpf.Controls;
using Bpf.Input;
using Bpf.Media;

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
        var window = app.CreateWindow(600, 450);
        window.Title = "bpf 布局面板演示 (M2.1)";

        // ── 用 Grid 做两行一列:顶行标题(50px),底行(*)填满剩余 ──
        var grid = new Grid();
        grid.Rows = "50,*";
        grid.Columns = "*";

        // 顶行:标题(放在第 0 行)
        var title = new TextBlock
        {
            Text = "bpf 布局面板演示",
            FontSize = 20,
            Foreground = new SolidColorBrush(Color.Black),
        };
        Grid.SetRow(title, 0);
        grid.AddChild(title);

        // 底行:DockPanel(放在第 1 行)
        var dock = new DockPanel();
        Grid.SetRow(dock, 1);
        grid.AddChild(dock);

        // ── DockPanel:顶部停靠提示,Bottom 停靠 Border 包裹的计数器,中间 Fill 放 Canvas ──
        var hint = new TextBlock
        {
            Text = "Canvas 上有两个按钮(绝对定位),点击试试",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
        };
        DockPanel.SetDock(hint, Dock.Top);
        dock.AddChild(hint);

        // Bottom:用 Border 包裹计数器(带圆角边框)
        var counterBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xFF)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0xCC)),
            BorderThickness = 1,
            CornerRadius = 4,
            Padding = 6,
        };
        var counter = new TextBlock
        {
            Text = "点击次数:0",
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x88)),
        };
        counterBorder.Child = counter;
        DockPanel.SetDock(counterBorder, Dock.Bottom);
        dock.AddChild(counterBorder);

        // Fill(LastChildFill 默认 true):Canvas
        var canvas = new Canvas();
        dock.AddChild(canvas);

        // ── Canvas:两个绝对定位的按钮 ──
        var incBtn = new Button { Content = "加一 (+)", FontSize = 14 };
        Canvas.SetLeft(incBtn, 30);
        Canvas.SetTop(incBtn, 30);
        Canvas.SetZIndex(incBtn, 1);
        canvas.AddChild(incBtn);

        var decBtn = new Button { Content = "减一 (-)", FontSize = 14 };
        Canvas.SetLeft(decBtn, 180);
        Canvas.SetTop(decBtn, 60);
        Canvas.SetZIndex(decBtn, 2); // 高 ZIndex,在上层
        canvas.AddChild(decBtn);

        // ── 事件 ──
        incBtn.Click += (s, e) => { _clickCount++; counter.Text = $"点击次数:{_clickCount}"; };
        decBtn.Click += (s, e) => { _clickCount--; counter.Text = $"点击次数:{_clickCount}"; };

        // 设为根面板
        window.SetContent(grid);

        app.Run();
    }

    private static int _clickCount;
}
