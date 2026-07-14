using System;
using System.Collections.Generic;

namespace Bpf.Controls.Routing
{
    /// <summary>
    /// 事件路由:构造从 root → target 的控件链,并按路由策略派发事件。
    /// M2 主要实现 Bubble;Tunnel/Direct 已支持但 Tunnel 的"Preview 前缀"语义留待 M3。
    /// </summary>
    internal static class EventRoute
    {
        /// <summary>
        /// 派发一个路由事件给 target 及其祖先。按 event 的 RoutingStrategy 决定遍历方向。
        /// </summary>
        /// <typeparam name="TArgs">事件参数类型。</typeparam>
        /// <param name="evt">路由事件标识。</param>
        /// <param name="e">事件参数(Source/OriginalSource 已设为 target)。</param>
        /// <param name="target">事件源控件。</param>
        public static void Dispatch<TArgs>(RoutedEvent<TArgs> evt, TArgs e, Control target)
            where TArgs : RoutedEventArgs
        {
            if (evt.RoutingStrategy == RoutingStrategies.Direct)
            {
                // Direct:只派发给 target 本身
                Deliver(target);
                return;
            }

            // 构造控件链:target → ... → root(Bubble 顺序)
            var chain = BuildAncestorChain(target);

            if ((evt.RoutingStrategy & RoutingStrategies.Tunnel) != 0)
            {
                // Tunnel:root → target(链的反向)
                for (int i = chain.Count - 1; i >= 0 && !e.Handled; i--)
                {
                    Deliver(chain[i]);
                }
            }

            if ((evt.RoutingStrategy & RoutingStrategies.Bubble) != 0)
            {
                // Bubble:target → root(链的正向)
                foreach (var c in chain)
                {
                    if (e.Handled) break;
                    Deliver(c);
                }
            }

            // 局部函数:派发给单个控件(更新 Source + 调虚方法 + 调订阅的 handler)
            void Deliver(Control c)
            {
                e.Source = c;
                c.DeliverEvent(evt, e);
            }
        }

        /// <summary>
        /// 构造 target 到 root 的祖先链(含 target 自身)。链[0]=target,链[last]=root(Window)。
        /// </summary>
        private static List<Control> BuildAncestorChain(Control target)
        {
            var chain = new List<Control>();
            Control? current = target;
            while (current is not null)
            {
                chain.Add(current);
                current = current.Parent;
            }
            return chain;
        }
    }
}
