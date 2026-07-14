using System;

namespace Bpf.PropertySystem
{
    /// <summary>
    /// 样式化属性。带默认值,可被样式/绑定覆盖。AOT 友好:每个声明独立泛型实例。
    /// </summary>
    /// <typeparam name="TValue">属性值类型。</typeparam>
    public sealed class StyledProperty<TValue> : BpfProperty
    {
        /// <summary>默认值(在未设置任何值时返回)。</summary>
        public TValue DefaultValue { get; }

        internal StyledProperty(string name, Type ownerType, TValue defaultValue,
            bool affectsMeasure, bool affectsArrange, bool affectsRender)
            : base(name, typeof(TValue), ownerType)
        {
            DefaultValue = defaultValue;
            AffectsMeasure = affectsMeasure;
            AffectsArrange = affectsArrange;
            AffectsRender = affectsRender;
        }

        /// <summary>
        /// 注册一个 StyledProperty。owner 类型通过泛型参数在编译期确定,
        /// NativeAOT 对每个 (TOwner, TValue) 组合生成独立代码。
        /// </summary>
        public static StyledProperty<TValue> Register<TOwner>(
            string name,
            TValue defaultValue,
            bool affectsMeasure = false,
            bool affectsArrange = false,
            bool affectsRender = false)
            where TOwner : class
        {
            return new StyledProperty<TValue>(name, typeof(TOwner), defaultValue,
                affectsMeasure, affectsArrange, affectsRender);
        }

        /// <summary>
        /// 注册一个 StyledProperty,显式传 owner 类型(用于不便用泛型的场景,
        /// 例如从一个非泛型上下文注册)。优先使用 <see cref="Register{TOwner}"/>。
        /// </summary>
        public static StyledProperty<TValue> Register(
            string name,
            Type ownerType,
            TValue defaultValue,
            bool affectsMeasure = false,
            bool affectsArrange = false,
            bool affectsRender = false)
        {
            return new StyledProperty<TValue>(name, ownerType, defaultValue,
                affectsMeasure, affectsArrange, affectsRender);
        }
    }
}
