using System;

namespace Bpf.Controls.Routing
{
    /// <summary>
    /// 路由事件策略。
    /// - <see cref="Bubble"/>:从事件源控件向其祖先逐级冒泡(最常用)。
    /// - <see cref="Tunnel"/>:从根向事件源逐级下沉,通常对应 Preview 前缀事件。
    /// - <see cref="Direct"/>:不路由,仅派发给事件源控件本身(类似普通 CLR 事件)。
    /// </summary>
    [Flags]
    public enum RoutingStrategies
    {
        /// <summary>下沉路由(根 → 源)。常用于 Preview 前缀事件。</summary>
        Tunnel = 1,

        /// <summary>冒泡路由(源 → 根)。默认策略。</summary>
        Bubble = 2,

        /// <summary>不路由,直接派发给源控件。</summary>
        Direct = 4,
    }
}
