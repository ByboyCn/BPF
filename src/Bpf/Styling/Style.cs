using System;
using System.Collections.Generic;
using Bpf.Controls;
using Bpf.PropertySystem;

namespace Bpf.Styling
{
    /// <summary>
    /// 样式:针对某控件类型 T 的一组属性覆盖。
    /// 挂在 Control.Styles 或 Window.Styles 上,被后代控件在 GetValue 时查找。
    /// 匹配规则:Style&lt;T&gt; 匹配类型 T 及其子类(WPF 风格)。
    /// </summary>
    public class Style
    {
        private readonly Type _targetType;
        private readonly List<Setter> _setters = new List<Setter>();

        /// <summary>样式针对的控件类型。该类型及其派生类会匹配此样式。</summary>
        public Type TargetType => _targetType;

        /// <summary>属性设置项列表。</summary>
        public IReadOnlyList<Setter> Setters => _setters;

        public Style(Type targetType)
        {
            _targetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        }

        /// <summary>添加一个强类型设置项。</summary>
        public Style Add<TValue>(StyledProperty<TValue> property, TValue value)
        {
            _setters.Add(new Setter<TValue>(property, value));
            return this;
        }

        /// <summary>添加一个已构造的设置项。</summary>
        public Style Add(Setter setter)
        {
            if (setter is null) throw new ArgumentNullException(nameof(setter));
            _setters.Add(setter);
            return this;
        }

        /// <summary>判断此样式是否适用于指定控件(控件类型是 TargetType 或其子类)。</summary>
        public bool Matches(Control control)
        {
            return _targetType.IsAssignableFrom(control.GetType());
        }

        /// <summary>在此样式中查找指定属性的设置值。无则返回 null。</summary>
        internal Setter? FindSetter(BpfProperty property)
        {
            foreach (var s in _setters)
            {
                if (ReferenceEquals(s.Property, property)) return s;
            }
            return null;
        }
    }

    /// <summary>
    /// 强类型样式构造便捷类。构造时确定 TargetType,Add 推断属性所属类型。
    /// 用法:<c>new Style&lt;Button&gt;().Add(Button.BackgroundProperty, brush)</c>
    /// </summary>
    public class Style<T> : Style where T : Control
    {
        public Style() : base(typeof(T)) { }
    }
}
