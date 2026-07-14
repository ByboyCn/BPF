using System;
using System.Collections.Generic;
using Bpf.Controls;
using Bpf.Controls.Routing;

namespace Bpf.Input
{
    /// <summary>
    /// 全局焦点管理器。维护当前焦点控件,提供 Tab 键导航。
    /// M2 简化版:单焦点,Tab 按深度优先遍历所有 IsFocusable 控件。
    /// </summary>
    public static class FocusManager
    {
        private static Control? _focused;

        /// <summary>当前拥有焦点的控件(null = 无焦点)。</summary>
        public static Control? Focused => _focused;

        /// <summary>
        /// 把焦点设置到指定控件。触发前一个控件的 LostFocus 和新控件的 GotFocus。
        /// 返回是否成功(控件必须 IsFocusable 且已挂载)。
        /// </summary>
        public static bool SetFocus(Control? control)
        {
            if (control is null)
            {
                ClearFocus();
                return true;
            }
            if (!control.IsFocusable) return false;

            var old = _focused;
            if (ReferenceEquals(old, control)) return true;

            _focused = control;

            // 先 LostFocus(旧控件)
            if (old is not null)
            {
                old.IsFocused = false;
                RaiseFocusEvent(old, Control.LostFocusEvent, old, control);
            }

            // 再 GotFocus(新控件)
            control.IsFocused = true;
            RaiseFocusEvent(control, Control.GotFocusEvent, old, control);

            return true;
        }

        /// <summary>清除当前焦点。</summary>
        public static void ClearFocus()
        {
            var old = _focused;
            if (old is null) return;
            _focused = null;
            old.IsFocused = false;
            RaiseFocusEvent(old, Control.LostFocusEvent, old, null);
        }

        /// <summary>
        /// 在指定根控件子树内,把焦点切换到下一个 IsFocusable 控件(Tab 顺序)。
        /// 如果当前无焦点,聚焦第一个可聚焦控件。返回新焦点控件(null = 无可聚焦控件)。
        /// </summary>
        public static Control? TabNext(Control root)
        {
            var focusables = new List<Control>();
            CollectFocusable(root, focusables);
            if (focusables.Count == 0) return null;

            if (_focused is null || !focusables.Contains(_focused))
            {
                SetFocus(focusables[0]);
                return _focused;
            }

            int idx = focusables.IndexOf(_focused);
            int next = (idx + 1) % focusables.Count;
            SetFocus(focusables[next]);
            return _focused;
        }

        /// <summary>反向 Tab(Shift+Tab)。</summary>
        public static Control? TabPrevious(Control root)
        {
            var focusables = new List<Control>();
            CollectFocusable(root, focusables);
            if (focusables.Count == 0) return null;

            if (_focused is null || !focusables.Contains(_focused))
            {
                SetFocus(focusables[focusables.Count - 1]);
                return _focused;
            }

            int idx = focusables.IndexOf(_focused);
            int prev = (idx - 1 + focusables.Count) % focusables.Count;
            SetFocus(focusables[prev]);
            return _focused;
        }

        // ── 辅助 ──

        private static void CollectFocusable(Control c, List<Control> result)
        {
            if (!c.IsVisible) return;
            if (c.IsFocusable) result.Add(c);
            if (c is IPanel panel)
            {
                foreach (var child in panel.Children)
                {
                    CollectFocusable(child, result);
                }
            }
        }

        private static void RaiseFocusEvent(Control target,
            RoutedEvent<FocusChangedEventArgs> evt,
            Control? oldFocus, Control? newFocus)
        {
            var e = new FocusChangedEventArgs(oldFocus, newFocus);
            target.RaiseEvent(evt, e);
        }
    }
}
