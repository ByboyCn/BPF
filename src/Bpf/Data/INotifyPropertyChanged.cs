using System;
using System.Collections.Generic;

namespace Bpf.Data
{
    /// <summary>
    /// 数据对象实现此接口以支持属性变更通知(数据绑定的基础)。
    /// AOT 完全友好:纯事件驱动,无反射。
    /// </summary>
    public interface INotifyPropertyChanged
    {
        event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>属性变更事件处理器。</summary>
    public delegate void PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs e);

    /// <summary>属性变更事件参数。</summary>
    public sealed class PropertyChangedEventArgs : EventArgs
    {
        /// <summary>变更的属性名(null 或空 = 所有属性都变了)。</summary>
        public string? PropertyName { get; }

        public PropertyChangedEventArgs(string? propertyName) => PropertyName = propertyName;
    }

    /// <summary>
    /// INotifyPropertyChanged 实现助手:SetProperty 自动比较+赋值+触发通知。
    /// 用法:<code>private string _name; public string Name { get => _name; set => this.Set(ref _name, value); }</code>
    /// </summary>
    public static class NotifyPropertyChangedExtensions
    {
        /// <summary>
        /// 设置字段值,若变化则触发 PropertyChanged。返回是否发生了变化。
        /// </summary>
        public static bool Set<T>(
            this INotifyPropertyChanged sender,
            ref T field,
            T value,
            string propertyName,
            PropertyChangedEventHandler? handler)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            handler?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
