using System.Collections.Generic;

namespace Bpf.Markup.Generator.Model
{
    /// <summary>
    /// 已知类型表:把 .bpfaml 里的元素本地名映射到全限定 CLR 类型名。
    /// 这是 AOT 安全的关键 —— 不用 Type.GetType(string) 反射查找,直接静态映射。
    /// 阶段 A 先硬编码核心控件;后续可由特性(attribute)自动发现扩展。
    /// </summary>
    internal static class KnownTypes
    {
        /// <summary>默认命名空间 "bpf" 下的控件名 → 全限定名。</summary>
        public static readonly Dictionary<string, string> BpfControls = new Dictionary<string, string>
        {
            { "Window",        "Bpf.Controls.Window" },
            { "Button",        "Bpf.Controls.Button" },
            { "TextBlock",     "Bpf.Controls.TextBlock" },
            { "TextBox",       "Bpf.Controls.TextBox" },
            { "Label",         "Bpf.Controls.Label" },
            { "CheckBox",      "Bpf.Controls.CheckBox" },
            { "RadioButton",   "Bpf.Controls.RadioButton" },
            { "ListBox",       "Bpf.Controls.ListBox" },
            { "ComboBox",      "Bpf.Controls.ComboBox" },
            { "Slider",        "Bpf.Controls.Slider" },
            { "Image",         "Bpf.Controls.Image" },
            { "Border",        "Bpf.Controls.Border" },
            { "ScrollViewer",  "Bpf.Controls.ScrollViewer" },
            { "StackPanel",    "Bpf.Controls.StackPanel" },
            { "Grid",          "Bpf.Controls.Grid" },
            { "Canvas",        "Bpf.Controls.Canvas" },
            { "DockPanel",     "Bpf.Controls.DockPanel" },
            // M11 新控件
            { "Separator",     "Bpf.Controls.Separator" },
            { "ProgressBar",   "Bpf.Controls.ProgressBar" },
            { "GroupBox",      "Bpf.Controls.GroupBox" },
            { "Expander",      "Bpf.Controls.Expander" },
            // M12 新控件
            { "NumericUpDown", "Bpf.Controls.NumericUpDown" },
            { "TabControl",    "Bpf.Controls.TabControl" },
            { "TreeView",      "Bpf.Controls.TreeView" },
            // Bpf.Media
            { "SolidColorBrush", "Bpf.Media.SolidColorBrush" },
            // M6 特殊元素(非控件,发射器按 LocalName 特殊处理;FullTypeName 仅占位)
            { "Style", "Bpf.Styling.Style" },
            { "Setter", "Bpf.Styling.Setter" },
        };

        /// <summary>尝试解析默认命名空间下的元素名。</summary>
        public static bool TryResolve(string localName, out string fullTypeName) =>
            BpfControls.TryGetValue(localName, out fullTypeName);
    }

    /// <summary>
    /// 每个控件的内容放置策略(ContentProperty 的硬编码表)。
    /// 当一个元素直接包含子元素(非属性元素)时,用此表决定如何挂接子元素。
    /// 三种模式:
    ///   Panel      → IPanel.AddChild(child)  [StackPanel/Grid/Window/DockPanel/Canvas]
    ///   Child      → 控件.Child = child(单子) [Border/ScrollViewer]
    ///   ContentText→ 控件.Content/Text = 字符串 [Button/Label/TextBlock]
    /// </summary>
    internal enum ContentStrategy
    {
        None,        // 不接受内容
        PanelAddChild,
        ChildSingle, // 设 .Child 属性
        ContentString, // 设 .Content 字符串属性
        TextString,    // 设 .Text 字符串属性
    }

    /// <summary>类型 → 内容策略表。</summary>
    internal static class ContentStrategies
    {
        private static readonly Dictionary<string, ContentStrategy> _map = new Dictionary<string, ContentStrategy>
        {
            { "Bpf.Controls.Window",       ContentStrategy.PanelAddChild },
            { "Bpf.Controls.StackPanel",   ContentStrategy.PanelAddChild },
            { "Bpf.Controls.Grid",         ContentStrategy.PanelAddChild },
            { "Bpf.Controls.Canvas",       ContentStrategy.PanelAddChild },
            { "Bpf.Controls.DockPanel",    ContentStrategy.PanelAddChild },
            { "Bpf.Controls.Border",       ContentStrategy.ChildSingle },
            { "Bpf.Controls.ScrollViewer", ContentStrategy.ChildSingle },
            { "Bpf.Controls.GroupBox",     ContentStrategy.ChildSingle },
            { "Bpf.Controls.Expander",     ContentStrategy.ChildSingle },
            { "Bpf.Controls.TabControl",   ContentStrategy.PanelAddChild },
            { "Bpf.Controls.Button",       ContentStrategy.ContentString },
            { "Bpf.Controls.Label",        ContentStrategy.TextString },
            { "Bpf.Controls.TextBlock",    ContentStrategy.TextString },
            { "Bpf.Controls.CheckBox",     ContentStrategy.ContentString },
        };

        public static ContentStrategy Get(string fullTypeName) =>
            _map.TryGetValue(fullTypeName, out var s) ? s : ContentStrategy.None;
    }
}
