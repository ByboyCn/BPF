using System;

namespace Bpf.Controls.Routing
{
    /// <summary>
    /// 路由事件标识。与 <see cref="Bpf.PropertySystem.StyledProperty{TValue}"/> 设计同构:
    /// 每个声明是独立静态字段,泛型实例化让 NativeAOT 为每个 (TOwner, TArgs) 组合生成独立代码。
    /// 不使用运行时反射 registry。
    /// </summary>
    /// <typeparam name="TArgs">事件参数类型,必须派生自 <see cref="RoutedEventArgs"/>。</typeparam>
    public sealed class RoutedEvent<TArgs> where TArgs : RoutedEventArgs
    {
        /// <summary>全局唯一 id(用作订阅字典 key)。所有 TArgs 共享同一计数器(见 RoutedEventBase)。</summary>
        internal int Id { get; }

        /// <summary>事件名。</summary>
        public string Name { get; }

        /// <summary>声明该事件的宿主类型。</summary>
        public Type OwnerType { get; }

        /// <summary>事件参数运行时类型。</summary>
        public Type ArgsType { get; }

        /// <summary>路由策略。</summary>
        public RoutingStrategies RoutingStrategy { get; }

        private RoutedEvent(string name, Type ownerType, RoutingStrategies strategy)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
            ArgsType = typeof(TArgs);
            RoutingStrategy = strategy;
            Id = RoutedEventBase.NextId();
        }

        /// <summary>
        /// 注册一个路由事件。owner 类型由泛型参数在编译期确定,
        /// NativeAOT 对每个 (TOwner, TArgs) 组合生成独立代码。
        /// </summary>
        public static RoutedEvent<TArgs> Register<TOwner>(
            string name,
            RoutingStrategies routingStrategy = RoutingStrategies.Bubble)
            where TOwner : class
        {
            return new RoutedEvent<TArgs>(name, typeof(TOwner), routingStrategy);
        }

        public override string ToString() => $"{OwnerType.Name}.{Name}";
    }

    /// <summary>
    /// 非 泛型辅助类:提供跨所有 RoutedEvent&lt;TArgs&gt; 共享的全局 Id 计数器。
    /// 关键:如果计数器放在 RoutedEvent&lt;TArgs&gt; 里(per-generic-static),
    /// 不同 TArgs 的计数器各自独立,会导致 ClickEvent(RoutedEventArgs) 和
    /// KeyDownEvent(KeyEventArgs) 拿到相同 Id,从而在 _eventHandlers 字典里冲突。
    /// </summary>
    internal static class RoutedEventBase
    {
        private static int s_nextId;

        public static int NextId() =>
            System.Threading.Interlocked.Increment(ref s_nextId);
    }
}
