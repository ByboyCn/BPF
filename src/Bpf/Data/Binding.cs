using System;
using System.Reflection;

namespace Bpf.Data
{
    /// <summary>
    /// 绑定描述:从数据源(Source)沿属性路径(Path)读取值。
    /// 属性路径用反射读取(NativeAOT 支持 PropertyInfo.GetValue,但需要属性不被 trim)。
    /// 用法:<c>var b = new Binding(person, "Name");</c>
    /// </summary>
    public sealed class Binding
    {
        /// <summary>数据源对象。</summary>
        public object? Source { get; set; }

        /// <summary>属性路径(如 "Name" 或 "User.Age",点分隔)。</summary>
        public string Path { get; set; }

        /// <summary>绑定方向。</summary>
        public BindingMode Mode { get; set; }

        /// <summary>值转换器(源→目标方向)。null = 不转换。</summary>
        public IValueConverter? Converter { get; set; }

        /// <summary>转换器参数。</summary>
        public object? ConverterParameter { get; set; }

        public Binding(object? source, string path, BindingMode mode = BindingMode.OneWay)
        {
            Source = source;
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Mode = mode;
        }

        /// <summary>
        /// 从 Source 沿 Path 读取当前值。任一段为 null 返回 null。
        /// </summary>
        public object? Evaluate()
        {
            object? current = Source;
            var segments = Path.Split('.');
            foreach (var seg in segments)
            {
                if (current is null) return null;
                current = GetPropertyValue(current, seg);
            }
            return current;
        }

        /// <summary>把值沿 Path 反向写入源(TwoWay 绑定用)。</summary>
        public void SetValue(object? value)
        {
            object? current = Source;
            var segments = Path.Split('.');
            // 走到倒数第二段
            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (current is null) return;
                current = GetPropertyValue(current, segments[i]);
            }
            if (current is null) return;
            // 在最后一段上 SetValue
            SetPropertyValue(current, segments[^1], value);
        }

        // ── 反射属性访问(AOT 友好:仅读取/写入,不 Emit) ──

        private static object? GetPropertyValue(object obj, string propertyName)
        {
            if (obj is null) return null;
            try
            {
                var prop = obj.GetType().GetTypeInfo().GetDeclaredProperty(propertyName);
                return prop?.GetValue(obj);
            }
            catch
            {
                return null;
            }
        }

        private static void SetPropertyValue(object obj, string propertyName, object? value)
        {
            try
            {
                var prop = obj.GetType().GetTypeInfo().GetDeclaredProperty(propertyName);
                if (prop?.CanWrite == true)
                    prop.SetValue(obj, value);
            }
            catch
            {
                // 优雅降级:写入失败(只读属性/类型不匹配)不崩溃
            }
        }
    }
}
