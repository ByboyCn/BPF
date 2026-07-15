using System;

namespace Bpf.Data
{
    /// <summary>
    /// 绑定值转换器。在 BindingExpression.Refresh 时 Convert(源→目标),
    /// TwoWay 的 PushBack 时 ConvertBack(目标→源)。
    /// </summary>
    public interface IValueConverter
    {
        /// <summary>源值 → 目标值(源数据变化时调用)。</summary>
        object? Convert(object? value, Type targetType, object? parameter);

        /// <summary>目标值 → 源值(TwoWay 绑定的 UI→数据回写)。</summary>
        object? ConvertBack(object? value, Type targetType, object? parameter);
    }

    /// <summary>
    /// 示例转换器:把 double 格式化为字符串(F0 = 整数)。
    /// 用法:Binding.Converter = new DoubleToStringConverter("F0");
    /// </summary>
    public sealed class DoubleToStringConverter : IValueConverter
    {
        private readonly string _format;

        public DoubleToStringConverter(string format = "F0") => _format = format;

        public object? Convert(object? value, Type targetType, object? parameter)
        {
            if (value is double d) return d.ToString(_format);
            if (value is int i) return i.ToString();
            return value?.ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter)
        {
            if (value is string s && double.TryParse(s, out var d)) return d;
            return value;
        }
    }

    /// <summary>
    /// 示例转换器:把任意值转为其字符串表示(调用 ToString())。
    /// </summary>
    public sealed class ToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter)
            => value?.ToString() ?? "";

        public object? ConvertBack(object? value, Type targetType, object? parameter)
            => value;
    }
}
