using System.Collections.Generic;

namespace Bpf.Markup.Generator.Model
{
    /// <summary>
    /// 事件名 → (CLR 事件属性所在控件类型简称, RoutedEvent 静态字段全名)。
    /// 用于发射器把 Click="OnClick" 转成 control.Click += MainForm.OnClick。
    ///
    /// 因为 bpf 所有事件都用标准 C# event(+= 订阅),且大多数是 EventHandler&lt;RoutedEventArgs&gt;,
    /// 生成器直接发射 += 即可,零反射。用户 code-behind 写对应签名的方法。
    ///
    /// 用户事件处理方法签名约定(静态,因 MainForm 是工厂模式):
    ///   private static void OnClick(object sender, global::Bpf.Controls.Routing.RoutedEventArgs e)
    /// </summary>
    internal static class KnownEvents
    {
        /// <summary>事件名 → 该事件的 CLR event 访问器名(同名)。</summary>
        /// bpf 的 RoutedEvent 字段命名是 XxxEvent,CLR event 访问器是 Xxx(去掉 Event)。
        /// 例如 Button.ClickEvent 字段 / Button.Click event 访问器。
        /// 生成器用 CLR event 访问器(+= 语法最简洁)。
        private static readonly HashSet<string> _knownEventNames = new HashSet<string>
        {
            "Click",            // Button
            "Checked",          // CheckBox, RadioButton
            "Unchecked",        // CheckBox, RadioButton
            "SelectionChanged", // ListBox, ComboBox
            "TextChanged",      // TextBox
            "ValueChanged",     // Slider
            "KeyDown", "KeyUp", "TextInput", // Control 输入
            "MouseWheel",       // Control
            "GotFocus", "LostFocus", // Control
        };

        /// <summary>判断属性名是否是已知事件(而非普通属性)。</summary>
        public static bool IsEvent(string name) => _knownEventNames.Contains(name);
    }
}
