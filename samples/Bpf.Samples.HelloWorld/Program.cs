using System;
using Bpf;
using Bpf.Controls;
using Bpf.Controls.Routing;
using Bpf.Input;
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
        var window = app.CreateWindow(480, 400);
        window.Title = "bpf Hello World";

        // 3. 内容:标题 + 计数器 + 两个按钮(演示 Tab 切焦点)
        var title = new TextBlock
        {
            Text = "你好,bpf!",
            FontSize = 24,
            Foreground = new SolidColorBrush(Color.Black),
        };
        window.AddChild(title);

        var hint = new TextBlock
        {
            Text = "Tab 切焦点,Enter/Space 触发,Esc 重置",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
        };
        window.AddChild(hint);

        var counter = new TextBlock
        {
            Text = "点击次数:0",
            FontSize = 16,
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
        };
        window.AddChild(counter);

        var incBtn = new Button { Content = "加一 (+)", FontSize = 14 };
        var decBtn = new Button { Content = "减一 (-)", FontSize = 14 };
        var keyLog = new TextBlock
        {
            Text = "(等待键盘事件冒泡...)",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x44, 0x00)),
        };

        // 4. 事件:按钮点击改变计数
        incBtn.Click += (s, e) =>
        {
            _clickCount++;
            counter.Text = $"点击次数:{_clickCount}";
        };
        decBtn.Click += (s, e) =>
        {
            _clickCount--;
            counter.Text = $"点击次数:{_clickCount}";
        };

        window.AddChild(incBtn);
        window.AddChild(decBtn);
        window.AddChild(keyLog);

        // 5. 路由事件冒泡演示:在窗口层订阅 KeyDown,任何子控件按键都会冒泡到这里
        window.AddHandler(Control.KeyDownEvent, new EventHandler<KeyEventArgs>((s, e) =>
        {
            // 这里收到的 e.Source 是冒泡路径上当前控件,OriginalSource 是最初触发者
            var src = e.OriginalSource?.GetType().Name ?? "?";
            keyLog.Text = $"[冒泡] 键={e.Key} 修饰={e.Modifiers} 源={src}";
        }));

        // 6. 演示 AddHandler 订阅 Click 的冒泡:窗口层也能收到按钮的 Click
        window.AddHandler(Button.ClickEvent, new EventHandler<RoutedEventArgs>((s, e) =>
        {
            var src = e.OriginalSource;
            System.Console.Error.WriteLine($"[bpf] 窗口层收到 Click 冒泡,源={src?.GetType().Name}");
            System.Console.Error.Flush();
        }));

        // 7. 运行
        app.Run();
    }

    private static int _clickCount;
}
