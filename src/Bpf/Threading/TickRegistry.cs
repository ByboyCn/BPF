using System;
using System.Collections.Generic;

namespace Bpf.Threading
{
    /// <summary>
    /// 每帧 tick 接口。由 <see cref="TickRegistry"/> 在每帧驱动,dt 为距上一帧的秒数。
    /// 用于光标闪烁、动画、定时刷新等需要每帧推进的逻辑。
    /// 返回 true 表示仍需继续 tick;false 表示完成(自动注销)。
    /// </summary>
    public interface ITickable
    {
        bool Tick(double dtSeconds);
    }

    /// <summary>
    /// 全局 tick 注册表。由 Window.RenderFrame 每帧调用 <see cref="TickAll"/>。
    /// 控件(如 TextBox 光标闪烁)或动画系统在此注册 ITickable。
    /// 线程:仅在主线程(UI 线程)使用,无需加锁。
    /// </summary>
    public static class TickRegistry
    {
        private static readonly List<ITickable> _tickables = new List<ITickable>();
        private static readonly List<ITickable> _pendingAdd = new List<ITickable>();
        private static double _lastTicksSeconds;
        private static bool _hasLast;

        /// <summary>注册一个 tickable。下一帧起开始驱动。</summary>
        public static void Register(ITickable t)
        {
            if (t is null) return;
            if (!_pendingAdd.Contains(t) && !_tickables.Contains(t))
                _pendingAdd.Add(t);
        }

        /// <summary>注销一个 tickable。</summary>
        public static void Unregister(ITickable t)
        {
            _tickables.Remove(t);
            _pendingAdd.Remove(t);
        }

        /// <summary>每帧由 Window.RenderFrame 调用,推进所有 tickable。</summary>
        public static void TickAll()
        {
            // 合并待添加(避免在遍历中修改列表)
            if (_pendingAdd.Count > 0)
            {
                foreach (var t in _pendingAdd) _tickables.Add(t);
                _pendingAdd.Clear();
            }

            double now = DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond;
            double dt = _hasLast ? (now - _lastTicksSeconds) : 0;
            _lastTicksSeconds = now;
            _hasLast = true;

            // 倒序遍历,便于删除已完成的
            for (int i = _tickables.Count - 1; i >= 0; i--)
            {
                var t = _tickables[i];
                bool keep;
                try { keep = t.Tick(dt); }
                catch { keep = false; }
                if (!keep) _tickables.RemoveAt(i);
            }
        }
    }
}
