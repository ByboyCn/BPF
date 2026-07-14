using System;

namespace Bpf.PropertySystem
{
    /// <summary>
    /// 所有可样式化属性的基类。作为 <see cref="Utilities.PropertyValueStore"/> 的 key,
    /// 同时承载属性的运行时元数据(名称、所属类型、值类型)。
    /// </summary>
    /// <remarks>
    /// AOT 设计说明:本类型本身不带反射注册。每个派生泛型实例
    /// (StyledProperty&lt;string&gt;、DirectProperty&lt;Foo, int&gt; 等)
    /// 都是独立静态字段,NativeAOT 会在每个具体 owner 上为它生成独立代码并保留。
    /// 不使用 Avalonia 的 AvaloniaPropertyRegistry 运行时反射注册机制。
    /// </remarks>
    public abstract class BpfProperty : IEquatable<BpfProperty>
    {
        private static int s_nextId;

        /// <summary>
        /// 全局唯一 id,用作 PropertyValueStore 的字典 key。每个实例递增。
        /// </summary>
        internal int Id { get; }

        /// <summary>属性名(通常与 CLR 包装器同名)。</summary>
        public string Name { get; }

        /// <summary>属性值运行时类型(用于类型校验、AOT 友好,不做反射注册)。</summary>
        public Type PropertyType { get; }

        /// <summary>声明该属性的宿主类型(Owner)。</summary>
        public Type OwnerType { get; }

        protected BpfProperty(string name, Type propertyType, Type ownerType)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            PropertyType = propertyType ?? throw new ArgumentNullException(nameof(propertyType));
            OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
            Id = System.Threading.Interlocked.Increment(ref s_nextId);
        }

        /// <summary>是否会影响控件的视觉度量(用于触发 MeasureInvalidated)。</summary>
        public bool AffectsMeasure { get; protected set; }

        /// <summary>是否会影响控件的视觉排列(用于触发 ArrangeInvalidated)。</summary>
        public bool AffectsArrange { get; protected set; }

        /// <summary>是否会影响控件的渲染(用于触发 RenderInvalidated)。</summary>
        public bool AffectsRender { get; protected set; }

        public bool Equals(BpfProperty? other) => other is not null && Id == other.Id;

        public override bool Equals(object? obj) => obj is BpfProperty p && Id == p.Id;

        public override int GetHashCode() => Id;

        public static bool operator ==(BpfProperty? left, BpfProperty? right) =>
            ReferenceEquals(left, right) || (left is not null && left.Equals(right));

        public static bool operator !=(BpfProperty? left, BpfProperty? right) =>
            !(left == right);

        public override string ToString() => $"{OwnerType.Name}.{Name}";
    }
}
