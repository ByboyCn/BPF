using System.Collections.Generic;

namespace Bpf.Markup.Generator.Model
{
    /// <summary>
    /// (控件全限定类型, 属性名) → StyledProperty 字段的全限定名(如 "Bpf.Controls.TextBlock.TextProperty")。
    /// 用于编译式绑定:SetCompiledBinding 需要 StyledProperty&lt;TValue&gt; 参数。
    ///
    /// 多数属性在控件自身声明(XxxProperty)。少数从基类继承(如 FontSize 各控件各自声明)。
    /// 此表手工维护已知控件的可绑定属性 → 字段位置。
    /// 未列入的 (类型,属性) 组合:绑定会跳过并(后续)发诊断。
    /// </summary>
    internal static class PropertyFields
    {
        // key = "FullTypeName.PropertyName",value = 字段全限定名
        private static readonly Dictionary<string, string> _fields = new Dictionary<string, string>
        {
            // TextBlock
            ["Bpf.Controls.TextBlock.Text"] = "Bpf.Controls.TextBlock.TextProperty",
            ["Bpf.Controls.TextBlock.FontSize"] = "Bpf.Controls.TextBlock.FontSizeProperty",
            ["Bpf.Controls.TextBlock.Foreground"] = "Bpf.Controls.TextBlock.ForegroundProperty",
            ["Bpf.Controls.TextBlock.FontWeight"] = "Bpf.Controls.TextBlock.FontWeightProperty",
            ["Bpf.Controls.TextBlock.FontFamily"] = "Bpf.Controls.TextBlock.FontFamilyProperty",
            // Label
            ["Bpf.Controls.Label.Text"] = "Bpf.Controls.Label.TextProperty",
            ["Bpf.Controls.Label.FontSize"] = "Bpf.Controls.Label.FontSizeProperty",
            ["Bpf.Controls.Label.Foreground"] = "Bpf.Controls.Label.ForegroundProperty",
            // TextBox
            ["Bpf.Controls.TextBox.Text"] = "Bpf.Controls.TextBox.TextProperty",
            ["Bpf.Controls.TextBox.FontSize"] = "Bpf.Controls.TextBox.FontSizeProperty",
            // Button
            ["Bpf.Controls.Button.Content"] = "Bpf.Controls.Button.ContentProperty",
            ["Bpf.Controls.Button.Background"] = "Bpf.Controls.Button.BackgroundProperty",
            ["Bpf.Controls.Button.Foreground"] = "Bpf.Controls.Button.ForegroundProperty",
            ["Bpf.Controls.Button.FontSize"] = "Bpf.Controls.Button.FontSizeProperty",
            // CheckBox
            ["Bpf.Controls.CheckBox.IsChecked"] = "Bpf.Controls.CheckBox.IsCheckedProperty",
            ["Bpf.Controls.CheckBox.Content"] = "Bpf.Controls.CheckBox.ContentProperty",
            // Slider
            ["Bpf.Controls.Slider.Value"] = "Bpf.Controls.Slider.ValueProperty",
            ["Bpf.Controls.Slider.Minimum"] = "Bpf.Controls.Slider.MinimumProperty",
            ["Bpf.Controls.Slider.Maximum"] = "Bpf.Controls.Slider.MaximumProperty",
            // Image
            ["Bpf.Controls.Image.Source"] = "Bpf.Controls.Image.SourceProperty",
            ["Bpf.Controls.Image.Stretch"] = "Bpf.Controls.Image.StretchProperty",
            // Border
            ["Bpf.Controls.Border.Background"] = "Bpf.Controls.Border.BackgroundProperty",
            // ComboBox / ListBox(SelectedIndex 是 CLR 属性,不是 StyledProperty;绑定暂不支持)
        };

        /// <summary>查询属性的 StyledProperty 字段全名。找不到返回 null。</summary>
        public static string? Find(string fullTypeName, string propName)
        {
            string key = fullTypeName + "." + propName;
            return _fields.TryGetValue(key, out var f) ? f : null;
        }
    }
}
