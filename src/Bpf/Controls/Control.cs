using System;
using System.Collections.Generic;
using Bpf.Controls.Routing;
using Bpf.Input;
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
        // 路由事件订阅存储:key=RoutedEvent(用其 Id),value=该事件的 handler 列表。
        // 用非泛型 RoutedEvent 键比较(通过 Id),handler 存为 Delegate。
        private Dictionary<int, List<Delegate>>? _eventHandlers;
        // 样式列表(应用于自身及后代控件)
        internal List<Styling.Style>? _styles;
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
                    OnParentChanged(value!);
                }
            }
        }

        /// <summary>逻辑树根(通常是 Window)。</summary>
        public Control? LogicalRoot => _logicalRoot;

        /// <summary>承载此控件的窗口(null = 未挂到窗口)。</summary>
        public IPlatformWindow? HostWindow => _hostWindow;

        /// <summary>
        /// 应用到此控件及其后代的样式列表。GetValue 查找时,后代会沿祖先链搜索匹配的 Style。
        /// 加第一个样式时延迟初始化内部列表。
        /// </summary>
        public System.Collections.Generic.List<Styling.Style> Styles
        {
            get => _styles ??= new System.Collections.Generic.List<Styling.Style>();
        }

        // ── 属性系统:StyledProperty 的 GetValue/SetValue ──────────────────────

        /// <summary>
        /// <summary>
        /// 读取 StyledProperty/AttachedProperty 值。
        /// 查找顺序:本地值 → 自身+祖先的 Styles(匹配的 Setter)→ 默认值。
        /// 设为 public 以支持附加属性(Grid.GetRow(child) 需从外部读子控件值)。
        /// </summary>
        public TValue GetValue<TValue>(StyledProperty<TValue> property)
        {
            // 1. 本地显式值优先
            if (_values is not null && _values.TryGetValue(property, out var boxed))
            {
                return (TValue)boxed!;
            }
            // 2. 查样式:自身 + 祖先链(最近祖先优先)
            var setter = FindStyleSetter(property);
            if (setter is not null)
            {
                return (TValue)setter.Value!;
            }
            // 3. 默认值
            return property.DefaultValue;
        }

        /// <summary>
        /// 从自身和祖先链查找匹配的 Style Setter。
        /// 查找顺序:自身 Styles → Parent.Styles → ... → root(最近祖先优先)。
        /// </summary>
        private Styling.Setter? FindStyleSetter(BpfProperty property)
        {
            Control? c = this;
            while (c is not null)
            {
                if (c._styles is not null)
                {
                    foreach (var style in c._styles)
                    {
                        if (style.Matches(this))
                        {
                            var s = style.FindSetter(property);
                            if (s is not null) return s;
                        }
                    }
                }
                c = c.Parent;
            }
            return null;
        }

        /// <summary>
        /// 写入 StyledProperty/AttachedProperty 值。变更会触发 AffectsMeasure/Arrange/Render 失效。
        /// 设为 public 以支持附加属性(Grid.SetRow(child, 1) 需从外部写子控件值)。
        /// </summary>
        public void SetValue<TValue>(StyledProperty<TValue> property, TValue value)
        {
            _values ??= new PropertyValueStore();

            if (_values.SetValue(property, value, out var oldValue))
            {
                OnPropertyValueChanged<TValue>(property, oldValue, value!);
            }
        }

        /// <summary>清空属性值,回落到默认值。</summary>
        public void ClearValue<TValue>(StyledProperty<TValue> property)
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

        // ── 输入:命中测试 + 指针(通过路由事件派发) ─────────────────────

        /// <summary>命中测试:点击坐标是否落在本控件内(或其子控件)。</summary>
        public virtual bool HitTest(Point point) =>
            IsVisible && Bounds.Contains(point);

        public virtual void OnPointerPressed(PointerEventArgs e) { }
        public virtual void OnPointerReleased(PointerEventArgs e) { }
        public virtual void OnPointerMoved(PointerEventArgs e) { }

        // ── 输入:键盘(通过路由事件派发) ─────────────────────────────

        public static readonly RoutedEvent<KeyEventArgs> KeyDownEvent =
            RoutedEvent<KeyEventArgs>.Register<Control>(nameof(KeyDown), RoutingStrategies.Bubble);
        public static readonly RoutedEvent<KeyEventArgs> KeyUpEvent =
            RoutedEvent<KeyEventArgs>.Register<Control>(nameof(KeyUp), RoutingStrategies.Bubble);
        public static readonly RoutedEvent<TextEventArgs> TextInputEvent =
            RoutedEvent<TextEventArgs>.Register<Control>(nameof(TextInput), RoutingStrategies.Bubble);

        public event EventHandler<KeyEventArgs>? KeyDown
        {
            add => AddHandler(KeyDownEvent, value!);
            remove => RemoveHandler(KeyDownEvent, value!);
        }
        public event EventHandler<KeyEventArgs>? KeyUp
        {
            add => AddHandler(KeyUpEvent, value!);
            remove => RemoveHandler(KeyUpEvent, value!);
        }
        public event EventHandler<TextEventArgs>? TextInput
        {
            add => AddHandler(TextInputEvent, value!);
            remove => RemoveHandler(TextInputEvent, value!);
        }

        protected internal virtual void OnKeyDown(KeyEventArgs e) { }
        protected internal virtual void OnKeyUp(KeyEventArgs e) { }
        protected internal virtual void OnTextInput(TextEventArgs e) { }

        // ── 焦点 ──────────────────────────────────────────────

        /// <summary>是否可被聚焦(Tab 导航 / Focus())。</summary>
        public bool IsFocusable { get; set; }

        /// <summary>当前是否拥有焦点(由 FocusManager 维护)。</summary>
        public bool IsFocused { get; internal set; }

        /// <summary>请求聚焦本控件。返回是否成功(失败原因:未挂载或不可聚焦)。</summary>
        public bool Focus()
        {
            if (!IsAttachedToHost || !IsFocusable) return false;
            return FocusManager.SetFocus(this);
        }

        public static readonly RoutedEvent<FocusChangedEventArgs> GotFocusEvent =
            RoutedEvent<FocusChangedEventArgs>.Register<Control>(nameof(GotFocus), RoutingStrategies.Bubble);
        public static readonly RoutedEvent<FocusChangedEventArgs> LostFocusEvent =
            RoutedEvent<FocusChangedEventArgs>.Register<Control>(nameof(LostFocus), RoutingStrategies.Bubble);

        public event EventHandler<FocusChangedEventArgs>? GotFocus
        {
            add => AddHandler(GotFocusEvent, value!);
            remove => RemoveHandler(GotFocusEvent, value!);
        }
        public event EventHandler<FocusChangedEventArgs>? LostFocus
        {
            add => AddHandler(LostFocusEvent, value!);
            remove => RemoveHandler(LostFocusEvent, value!);
        }

        protected internal virtual void OnGotFocus(FocusChangedEventArgs e) { }
        protected internal virtual void OnLostFocus(FocusChangedEventArgs e) { }

        // ── 路由事件:订阅 + 派发 ──────────────────────────────────

        /// <summary>订阅一个路由事件(AOT 友好:每个 RoutedEvent&lt;T&gt; 是独立泛型实例)。</summary>
        public void AddHandler<TArgs>(RoutedEvent<TArgs> routedEvent, EventHandler<TArgs> handler)
            where TArgs : RoutedEventArgs
        {
            _eventHandlers ??= new Dictionary<int, List<Delegate>>();
            if (!_eventHandlers.TryGetValue(routedEvent.Id, out var list))
            {
                list = new List<Delegate>();
                _eventHandlers[routedEvent.Id] = list;
            }
            list.Add(handler);
        }

        /// <summary>取消订阅一个路由事件。</summary>
        public void RemoveHandler<TArgs>(RoutedEvent<TArgs> routedEvent, EventHandler<TArgs> handler)
            where TArgs : RoutedEventArgs
        {
            if (_eventHandlers is null) return;
            if (_eventHandlers.TryGetValue(routedEvent.Id, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0) _eventHandlers.Remove(routedEvent.Id);
            }
        }

        /// <summary>
        /// 在本控件上触发一个路由事件。框架构造从本控件到根的路由路径并按策略派发。
        /// 控件内部用此方法派发输入事件(如 Button 调 RaiseEvent(ClickEvent, ...))。
        /// 设为 internal 而非 protected,以便 Window(派生类但需对其他 Control 实例调用)
        /// 和 FocusManager(非派生类)都能使用。
        /// </summary>
        internal void RaiseEvent<TArgs>(RoutedEvent<TArgs> routedEvent, TArgs e)
            where TArgs : RoutedEventArgs
        {
            e.Reset(routedEvent, this);
            EventRoute.Dispatch(routedEvent, e, this);
        }

        /// <summary>
        /// 由 EventRoute 调用:把事件派发给本控件的虚方法 + 订阅的 handler。
        /// 顺序:先调 On* 虚方法(控件自身处理),再调外部订阅的 handler。
        /// </summary>
        internal void DeliverEvent<TArgs>(RoutedEvent<TArgs> routedEvent, TArgs e)
            where TArgs : RoutedEventArgs
        {
            // 调虚方法让控件类型自身先处理
            InvokeOnOverride(routedEvent, e);

            // 再调外部订阅的 handler
            if (_eventHandlers is not null && _eventHandlers.TryGetValue(routedEvent.Id, out var list))
            {
                // 复制一份避免订阅期间列表被修改
                var handlers = list.ToArray();
                foreach (var h in handlers)
                {
                    if (e.Handled) break;
                    ((EventHandler<TArgs>)h)(this, e);
                }
            }
        }

        /// <summary>
        /// 把路由事件分发到对应的 On* 虚方法。集中映射,避免在每个 DeliverEvent 里 switch。
        /// </summary>
        private void InvokeOnOverride<TArgs>(RoutedEvent<TArgs> routedEvent, TArgs e)
            where TArgs : RoutedEventArgs
        {
            switch (e)
            {
                case KeyEventArgs ke when ReferenceEquals(routedEvent, KeyDownEvent):
                    OnKeyDown(ke); break;
                case KeyEventArgs ke when ReferenceEquals(routedEvent, KeyUpEvent):
                    OnKeyUp(ke); break;
                case TextEventArgs te when ReferenceEquals(routedEvent, TextInputEvent):
                    OnTextInput(te); break;
                case PointerEventArgs pe:
                    // 指针事件不走 RoutedEvent 声明,而是通过 Position/Bucket 判断
                    // 实际指针路由在 Window 层用 RaisePointer* + EventRoute 完成
                    break;
                case FocusChangedEventArgs fe when ReferenceEquals(routedEvent, GotFocusEvent):
                    OnGotFocus(fe); break;
                case FocusChangedEventArgs fe when ReferenceEquals(routedEvent, LostFocusEvent):
                    OnLostFocus(fe); break;
            }
        }

        // ── 指针事件路由派发入口(由 Window 命中测试后调用) ──────────────

        internal void RaisePointerPressed(PointerEventArgs e)
        {
            e.Reset(null, this);
            OnPointerPressed(e);
        }

        internal void RaisePointerReleased(PointerEventArgs e)
        {
            e.Reset(null, this);
            OnPointerReleased(e);
        }

        internal void RaisePointerMoved(PointerEventArgs e)
        {
            e.Reset(null, this);
            OnPointerMoved(e);
        }
    }
}
