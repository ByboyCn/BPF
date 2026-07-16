using System.Collections.Generic;

namespace Bpf.Markup.Generator.Model
{
    /// <summary>
    /// 属性名 → CLR 类型分类。用于发射器决定如何把字符串值转成 C# 表达式。
    ///
    /// 设计依据:bpf 中同名属性的 CLR 类型在各控件间一致(FontSize 都是 double,
    /// Background/Foreground/BorderBrush 都是 Brush,IsChecked 都是 bool,…)。
    /// 因此只需一张全局表,不必按控件区分。例外用 <see cref="SpecialStringProps"/> 标注。
    /// </summary>
    internal enum PropType
    {
        Unknown,
        String,      // 直接字面量(已处理)
        Double,      // double.Parse
        Int,         // int.Parse
        Bool,        // bool.Parse
        Brush,       // Color.Parse → new SolidColorBrush
        Color,       // Color.Parse
        Stretch,     // Enum.Parse<Stretch>
        Orientation, // Enum.Parse<Orientation>
        FontWeight,  // Enum.Parse<FontWeight>
        FontStyle,   // Enum.Parse<FontStyle>
        GridLengthString, // Grid 的 Rows/Columns 字符串(直接赋值)
    }

    /// <summary>属性名 → 类型分类表。</summary>
    internal static class PropertyTypes
    {
        // 属性名 → 类型(同名同类型,跨控件一致)
        private static readonly Dictionary<string, PropType> _map = new Dictionary<string, PropType>
        {
            // 字符串类
            { "Content",   PropType.String },      // Button/CheckBox.Content 是 string
            { "Text",      PropType.String },      // TextBox/Label/TextBlock.Text
            { "Title",     PropType.String },      // Window.Title(注:Window 不经 bpfaml 实例化,通常用不到)
            { "Source",    PropType.String },      // Image.Source(文件路径)
            { "FontFamily",PropType.String },
            { "Color",     PropType.Color },      // SolidColorBrush.Color
            // 数字
            { "FontSize",  PropType.Double },
            { "Width",     PropType.Double },   // Layoutable.Width(显式宽度)
            { "Height",    PropType.Double },   // Layoutable.Height(显式高度)
            { "Padding",   PropType.Double },
            { "BorderThickness", PropType.Double },
            { "CornerRadius",    PropType.Double },
            { "Minimum",   PropType.Double },
            { "Maximum",   PropType.Double },
            { "Value",     PropType.Double },
            { "SelectedIndex", PropType.Int },
            // 布尔
            { "IsChecked", PropType.Bool },
            // 画刷/颜色
            { "Background", PropType.Brush },
            { "Foreground", PropType.Brush },
            { "BorderBrush", PropType.Brush },
            { "CheckMarkBrush", PropType.Brush },
            // 枚举
            { "Stretch",     PropType.Stretch },
            { "Orientation", PropType.Orientation },
            { "FontWeight",  PropType.FontWeight },
            { "FontStyle",   PropType.FontStyle },
            // Grid 行列定义(字符串便捷形式,直接赋值)
            { "Rows",    PropType.GridLengthString },
            { "Columns", PropType.GridLengthString },
        };

        /// <summary>查询属性类型。未知返回 Unknown。</summary>
        public static PropType Get(string propName) =>
            _map.TryGetValue(propName, out var t) ? t : PropType.Unknown;
    }
}
