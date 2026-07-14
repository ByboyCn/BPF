using System;
using Bpf;
using Bpf.Controls;
using Bpf.Media;

namespace Bpf.Samples.HelloWorld;

internal static class Program
{
    private static void Main()
    {
        try
        {
            RunApp();
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"[bpf FATAL] {ex}");
            System.Console.Error.Flush();
            throw;
        }
    }

    private static void RunApp()
    {
        // 1. 初始化 Windows 后端
        var app = Bpf.Windows.WindowsAppExtensions.UseWindows();

        // 2. 创建窗口
        var window = app.CreateWindow(480, 320);
        window.Title = "bpf Hello World";

        // 3. 内容:一个标题 TextBlock + 一个可点击 Button
        var title = new TextBlock
        {
            Text = "你好,bpf!",
            FontSize = 24,
            Foreground = new SolidColorBrush(Color.Black),
        };
        window.AddChild(title);

        var counter = new TextBlock
        {
            Text = "点击次数:0",
            FontSize = 16,
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
        };
        window.AddChild(counter);

        var button = new Button
        {
            Content = "点我",
            FontSize = 14,
        };
        button.Click += (s, e) =>
        {
            _clickCount++;
            counter.Text = $"点击次数:{_clickCount}";
        };
        window.AddChild(button);

        // 4. 运行
        app.Run();
    }

    private static int _clickCount;
}
