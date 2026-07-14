using System;
using System.Collections.Generic;
using Bpf.Platform;
using Bpf.PropertySystem;
using Bpf.Utilities;

namespace Bpf.Controls
{
    /// <summary>
    /// 控件基类:逻辑树节点 + 属性系统接入点。
    /// 所有面向用户的控件最终继承自 Control。
    /// </summary>
    public abstract class Control : Layoutable
    {
        private PropertyValueStore? _values;
        private Control? _parent;
        private Control? _logicalRoot;
        private IPlatformWindow? _hostWindow;

        /// <summary>逻辑父节点。</summary>
        public Control? Parent
        {
            get => _parent;
            internal set
            {
                if (_parent != value)
                {
                    _parent = value;
                    OnParentChanged(value);
                }
            }
        }

        /// <summary>逻辑树根(通常是 Window)。</summary>
        public Control? LogicalRoot => _logicalRoot;

        /// <summary>承载此控件的窗口(null = 未挂到窗口)。</summary>
        public IPlatformWindow? HostWindow => _hostWindow;

        // ── 属性系统:StyledProperty 的 GetValue/SetValue ──────────────────────

        /// <summary>
        /// 读取 StyledProperty 值。未显式设置时回落到属性的默认值。
        /// </summary>
        protected TValue GetValue<TValue>(StyledProperty<TValue> property)
        {
            if (_values is not null && _values.TryGetValue(property, out var boxed))
            {
                return (TValue)boxed!;
            }
            return property.DefaultValue;
        }

        /// <summary>
        /// 写入 StyledProperty 值。变更会触发 AffectsMeasure/Arrange/Render 失效。
        /// </summary>
        protected void SetValue<TValue>(StyledProperty<TValue> property, TValue value)
        {
            _values ??= new PropertyValueStore();

            if (_values.SetValue(property, value, out var oldValue))
            {
                OnPropertyValueChanged<TValue>(property, oldValue, value);
            }
        }

        /// <summary>清空属性值,回落到默认值。</summary>
        protected void ClearValue<TValue>(StyledProperty<TValue> property)
        {
            // M1 简化:直接置为默认值
            SetValue(property, property.DefaultValue);
        }

        /// <summary>属性值变更后的副作用处理:触发布局/渲染失效,并调用虚回调。</summary>
        private void OnPropertyValueChanged<TValue>(StyledProperty<TValue> property,
            object? oldValue, TValue newValue)
        {
            if (property.AffectsMeasure) InvalidateMeasure();
            if (property.AffectsArrange) InvalidateArrange();
            if (property.AffectsRender) InvalidateVisual();

            OnPropertyChanged<TValue>(property, oldValue, newValue);
        }

        /// <summary>子类重写以响应具体属性变更(如重新构造视觉子节点)。</summary>
        protected virtual void OnPropertyChanged<TValue>(StyledProperty<TValue> property,
            object? oldValue, TValue newValue)
        {
        }

        // ── 逻辑树挂载 ──────────────────────────────────────────────

        /// <summary>
        /// 把本控件挂到一个窗口上(成为逻辑树根)。由 Window 在初始化时调用。
        /// 内部会递归 attach 所有逻辑子节点,使整棵树都能访问到 HostWindow。
        /// </summary>
        internal void AttachToHost(IPlatformWindow window, Control? logicalRoot = null)
        {
            _hostWindow = window;
            _logicalRoot = logicalRoot ?? this;
            OnAttachedToHost();

            // 递归 attach 子节点(Panel/ContentControl 等)
            if (this is IPanel panel)
            {
                foreach (var child in panel.Children)
                {
                    child.AttachToHost(window, _logicalRoot);
                }
            }
            else
            {
                AttachNonPanelChildren(window, _logicalRoot);
            }
        }

        /// <summary>
        /// 非 Panel 控件重写此方法以 attach 自身的逻辑子节点(如 Button 内部的 TextBlock)。
        /// 默认实现为空。
        /// </summary>
        protected virtual void AttachNonPanelChildren(IPlatformWindow window, Control logicalRoot) { }

        /// <summary>从窗口卸载。</summary>
        internal void DetachFromHost()
        {
            OnDetachingFromHost();
            _hostWindow = null;
            _logicalRoot = null;
        }

        protected virtual void OnAttachedToHost() { }
        protected virtual void OnDetachingFromHost() { }

        protected virtual void OnParentChanged(Control? newParent)
        {
            // 重新计算逻辑根
            _logicalRoot = newParent?._logicalRoot ?? (IsAttachedToHost ? this : null);
        }

        /// <summary>是否已挂载到窗口。</summary>
        public bool IsAttachedToHost => _hostWindow is not null;

        /// <summary>请求窗口重绘。M1 通过事件冒泡到 WindowImpl。</summary>
        protected override void InvalidateVisual()
        {
            base.InvalidateVisual();
            _hostWindow?.Invalidate();
        }

        // ── 输入(M1 仅鼠标,通过窗口派发) ─────────────────────────────

        /// <summary>命中测试:点击坐标是否落在本控件内。M1 用 Bounds 简单判断。</summary>
        public virtual bool HitTest(Point point) =>
            IsVisible && Bounds.Contains(point);

        public virtual void OnPointerPressed(PointerEventArgs e) { }
        public virtual void OnPointerReleased(PointerEventArgs e) { }
        public virtual void OnPointerMoved(PointerEventArgs e) { }

        /// <summary>
        /// 公开事件版(供用户订阅)。M1 只提供 Pressed。
        /// </summary>
        public event EventHandler<PointerEventArgs>? PointerPressed;

        internal void RaisePointerPressed(PointerEventArgs e)
        {
            OnPointerPressed(e);
            PointerPressed?.Invoke(this, e);
        }

        internal void RaisePointerReleased(PointerEventArgs e)
        {
            OnPointerReleased(e);
        }

        internal void RaisePointerMoved(PointerEventArgs e)
        {
            OnPointerMoved(e);
        }
    }
}
