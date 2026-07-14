using System;

namespace Bpf.PropertySystem
{
    /// <summary>
    /// 附加属性。值存储在目标控件(子)上,但由属主类型(如 Grid/Canvas)定义和读写。
    /// 典型用法:<c>Grid.SetRow(child, 1)</c> 等价于 <c>child.SetValue(Grid.RowProperty, 1)</c>。
    /// 继承自 <see cref="StyledProperty{TValue}"/>,因此值的存储复用
    /// <see cref="Bpf.Utilities.PropertyValueStore"/>/<see cref="Bpf.Controls.Control.GetValue{TValue}"/>,
    /// 无需新存储结构。
    /// </summary>
    /// <typeparam name="TValue">属性值类型。</typeparam>
    public sealed class AttachedProperty<TValue> : StyledProperty<TValue>
    {
        private AttachedProperty(string name, Type ownerType, TValue defaultValue,
            bool affectsMeasure, bool affectsArrange, bool affectsRender)
            : base(name, ownerType, defaultValue, affectsMeasure, affectsArrange, affectsRender)
        {
        }

        /// <summary>
        /// 注册一个附加属性。owner 类型由泛型参数在编译期确定,
        /// NativeAOT 对每个 (TOwner, TValue) 组合生成独立代码。
        /// </summary>
        public static new AttachedProperty<TValue> Register<TOwner>(
            string name,
            TValue defaultValue,
            bool affectsMeasure = false,
            bool affectsArrange = false,
            bool affectsRender = false)
            where TOwner : class
        {
            return new AttachedProperty<TValue>(name, typeof(TOwner), defaultValue,
                affectsMeasure, affectsArrange, affectsRender);
        }

        /// <summary>注册附加属性,显式传 owner 类型。</summary>
        public static new AttachedProperty<TValue> Register(
            string name,
            Type ownerType,
            TValue defaultValue,
            bool affectsMeasure = false,
            bool affectsArrange = false,
            bool affectsRender = false)
        {
            return new AttachedProperty<TValue>(name, ownerType, defaultValue,
                affectsMeasure, affectsArrange, affectsRender);
        }
    }
}
