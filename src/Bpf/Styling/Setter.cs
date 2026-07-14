using System;
using Bpf.PropertySystem;

namespace Bpf.Styling
{
    /// <summary>
    /// 样式设置项:把一个 StyledProperty 设为指定值。
    /// 非泛型基类供 Style 统一存储;泛型子类保证类型安全。
    /// </summary>
    public abstract class Setter
    {
        /// <summary>目标属性(非泛型基类 BpfProperty)。</summary>
        public abstract BpfProperty Property { get; }

        /// <summary>属性值(boxed)。</summary>
        public abstract object? Value { get; }
    }

    /// <summary>
    /// 强类型设置项。
    /// </summary>
    public sealed class Setter<TValue> : Setter
    {
        private readonly StyledProperty<TValue> _property;
        private readonly TValue _value;

        public Setter(StyledProperty<TValue> property, TValue value)
        {
            _property = property ?? throw new ArgumentNullException(nameof(property));
            _value = value;
        }

        public override BpfProperty Property => _property;
        public override object? Value => _value;
    }
}
