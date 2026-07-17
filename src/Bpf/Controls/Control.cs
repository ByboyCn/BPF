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
        // 数据绑定:key=属性 Id,value=BindingExpression
        private Dictionary<int, Bpf.Data.BindingExpression>? _bindings;
        // 编译式绑定(M6):key=属性 Id,value=CompiledBindingExpression。优先级同 _bindings。
        private Dictionary<int, Bpf.Data.CompiledBindingExpression>? _compiledBindings;
        private Control? _parent;
        private Control? _logicalRoot;
        private IPlatformWindow? _hostWindow;
        // DataContext:数据绑定的源。null 时沿祖先链查找(继承)。
        private object? _dataContext;
        // 资源字典:key → 任意对象(Brush/Color/Style/...)。{StaticResource Key} 沿祖先链查找。
        private Dictionary<string, object>? _resources;

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
        /// 数据上下文:数据绑定的源对象。设置此属性后,本控件及未显式设置 DataContext 的后代控件,
        /// 其 {Binding} 会以此为源。沿祖先链查找(继承语义,类似 WPF/Avalonia)。
        /// </summary>
        public object? DataContext
        {
            get => _dataContext;
            set
            {
                if (!ReferenceEquals(_dataContext, value))
                {
                    _dataContext = value;
                    OnDataContextChanged(value);
                }
            }
        }

        /// <summary>
        /// 本控件的资源字典(键 → 对象)。{StaticResource Key} 会沿祖先链查找。
        /// 通常在容器(Window/StackPanel)的 Resources 里放共享的 Brush/Color,
        /// 后代控件用 {StaticResource Key} 引用,实现外观复用与主题化。
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> Resources
        {
            get => _resources ??= new System.Collections.Generic.Dictionary<string, object>();
        }

        /// <summary>
        /// 沿祖先链查找资源。找不到返回 false。
        /// </summary>
        public bool TryFindResource(string key, out object? resource)
        {
            Control? c = this;
            while (c is not null)
            {
                if (c._resources is not null && c._resources.TryGetValue(key, out resource))
                    return true;
                c = c._parent;
            }
            resource = null;
            return false;
        }

        /// <summary>
        /// 沿祖先链查找资源。找不到抛 KeyNotFoundException。
        /// </summary>
        public object FindResource(string key)
        {
            if (TryFindResource(key, out var r) && r is not null) return r;
            throw new System.Collections.Generic.KeyNotFoundException($"未找到资源: {key}");
        }

        /// <summary>
        /// 沿祖先链查找有效 DataContext(本控件未设则查父级)。
        /// </summary>
        public object? GetEffectiveDataContext()
        {
            Control? c = this;
            while (c is not null)
            {
                if (c._dataContext is not null) return c._dataContext;
                c = c._parent;
            }
            return null;
        }

        /// <summary>DataContext 变更时:通知本控件及后代的编译式绑定重新挂接新源。</summary>
        private void OnDataContextChanged(object? newDataContext)
        {
            // 重新挂接本控件的编译式绑定(用新 DataContext)
            if (_compiledBindings is not null)
            {
                foreach (var kv in _compiledBindings)
                    kv.Value.Rebind(newDataContext);
            }
            // 通知后代(它们的 GetEffectiveDataContext 结果变了)
            if (this is IPanel panel)
            {
                foreach (var child in panel.Children)
                    child.OnInheritedDataContextChanged();
            }
            else
            {
                OnInheritedDataContextChangedForNonPanelChildren();
            }
        }

        /// <summary>后代被通知祖先 DataContext 变了:若本控件未设自己的 DataContext,重新挂接绑定并向下传。</summary>
        internal void OnInheritedDataContextChanged()
        {
            // 若本控件自己设了 DataContext,继承链在此截断,不再向下传(本控件子树用自己的)
            if (_dataContext is not null) return;

            if (_compiledBindings is not null)
            {
                foreach (var kv in _compiledBindings)
                    kv.Value.Rebind(GetEffectiveDataContext());
            }
            if (this is IPanel panel)
            {
                foreach (var child in panel.Children)
                    child.OnInheritedDataContextChanged();
            }
            else
            {
                OnInheritedDataContextChangedForNonPanelChildren();
            }
        }

        /// <summary>非 Panel 控件重写此方法以通知其逻辑子(如 Border.Child)DataContext 变化。</summary>
        protected virtual void OnInheritedDataContextChangedForNonPanelChildren() { }

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

        // ── 数据绑定 ──────────────────────────────────────────

        /// <summary>
        /// 在指定属性上挂一个数据绑定。
        /// 绑定值优先于样式,仅次于本地显式值(SetValue)。
        /// </summary>
        public void SetBinding<TValue>(StyledProperty<TValue> property, Bpf.Data.Binding binding)
        {
            _bindings ??= new Dictionary<int, Bpf.Data.BindingExpression>();

            // 旧绑定 detach
            if (_bindings.TryGetValue(property.Id, out var old))
                old.Detach();

            var expr = new Bpf.Data.BindingExpression(this, property, binding);
            _bindings[property.Id] = expr;
            expr.Attach();

            // 触发布局/渲染失效以反映新值
            if (property.AffectsMeasure) InvalidateMeasure();
            if (property.AffectsArrange) InvalidateArrange();
            InvalidateVisual();
        }

        /// <summary>
        /// 在指定属性上挂一个编译式数据绑定(M6)。
        /// 编译式绑定由源生成器生成 lambda(get/set 访问器),不依赖反射,AOT 完全兼容。
        /// 源对象取自 GetEffectiveDataContext()(沿祖先链继承)。
        /// </summary>
        public void SetCompiledBinding<TValue>(
            StyledProperty<TValue> property, Bpf.Data.CompiledBinding binding)
        {
            _compiledBindings ??= new Dictionary<int, Bpf.Data.CompiledBindingExpression>();

            if (_compiledBindings.TryGetValue(property.Id, out var old))
                old.Detach();

            var expr = new Bpf.Data.CompiledBindingExpression(this, property, binding);
            _compiledBindings[property.Id] = expr;
            expr.Attach(GetEffectiveDataContext());

            if (property.AffectsMeasure) InvalidateMeasure();
            if (property.AffectsArrange) InvalidateArrange();
            InvalidateVisual();
        }

        /// <summary>
        /// BindingExpression 用:直接写本地值,绕过绑定回查(防递归)。
        /// </summary>
        internal void SetValueInternal(BpfProperty property, object? value)
        {
            _values ??= new Utilities.PropertyValueStore();
            _values.SetValue(property, value, out _);
            if (property.AffectsMeasure) InvalidateMeasure();
            if (property.AffectsArrange) InvalidateArrange();
            InvalidateVisual();
        }

        /// <summary>BindingExpression 用:请求重绘(internal,绕过 protected InvalidateVisual)。</summary>
        internal void RequestRedraw() => InvalidateVisual();

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

        /// <summary>
        /// 扩展命中测试:用于 RenderOnTop 控件(弹出下拉/调色板可能超出 Bounds)。
        /// 默认同 HitTest。控件重写以接受超出 Bounds 的弹出区域。
        /// point 是窗口坐标。
        /// </summary>
        public virtual bool HitTestExtended(Point point) => HitTest(point);

        public virtual void OnPointerPressed(PointerEventArgs e) { }
        public virtual void OnPointerReleased(PointerEventArgs e) { }
        public virtual void OnPointerMoved(PointerEventArgs e) { }

        /// <summary>
        /// 捕获指针:捕获后,后续的 PointerMoved/PointerReleased 都发给本控件(即使鼠标移出 Bounds)。
        /// 用于拖动场景(滑块、滚动条)。在 OnPointerPressed 里调用,OnPointerReleased 里释放。
        /// </summary>
        public void CapturePointer()
        {
            (LogicalRoot as Window)?.SetPointerCapture(this);
        }

        /// <summary>释放指针捕获。</summary>
        public void ReleasePointerCapture()
        {
            (LogicalRoot as Window)?.ClearPointerCapture(this);
        }

        /// <summary>指针(鼠标)当前是否悬停在本控件上。由 Window 在指针移动时维护。</summary>
        public bool IsPointerOver { get; internal set; }

        /// <summary>
        /// 工具提示文字。鼠标悬停在本控件上约 0.5 秒后,Window 在顶层绘制此文字(气泡)。
        /// 为空则不显示。可由 .bpfaml 设 ToolTip="..."。
        /// </summary>
        public string? ToolTip { get; set; }

        /// <summary>指针进入控件边界时调用(由 Window 在 hover 目标变化时触发)。</summary>
        protected internal virtual void OnPointerEntered(PointerEventArgs e) { }
        /// <summary>指针离开控件边界时调用。</summary>
        protected internal virtual void OnPointerExited(PointerEventArgs e) { }

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

        // ── 鼠标滚轮 ──

        public static readonly RoutedEvent<MouseWheelEventArgs> MouseWheelEvent =
            RoutedEvent<MouseWheelEventArgs>.Register<Control>(nameof(MouseWheel), RoutingStrategies.Bubble);

        public event EventHandler<MouseWheelEventArgs>? MouseWheel
        {
            add => AddHandler(MouseWheelEvent, value!);
            remove => RemoveHandler(MouseWheelEvent, value!);
        }

        protected internal virtual void OnMouseWheel(MouseWheelEventArgs e) { }

        // ── 焦点 ──────────────────────────────────────────────

        /// <summary>是否可被聚焦(Tab 导航 / Focus())。</summary>
        public bool IsFocusable { get; set; }

        /// <summary>
        /// 是否需要在同层控件之上渲染(最后画)。ComboBox 展开下拉时设为 true。
        /// 面板(Render/StackPanel/Grid 等)先把 RenderOnTop=false 的子控件画完,再画 true 的。
        /// </summary>
        internal bool RenderOnTop { get; set; }

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
                    // 维护全局修饰键状态(供 PointerPressed 等场景查询)
                    Bpf.Input.Keyboard.Modifiers = ke.Modifiers;
                    OnKeyDown(ke); break;
                case KeyEventArgs ke when ReferenceEquals(routedEvent, KeyUpEvent):
                    Bpf.Input.Keyboard.Modifiers = ke.Modifiers;
                    OnKeyUp(ke); break;
                case TextEventArgs te when ReferenceEquals(routedEvent, TextInputEvent):
                    OnTextInput(te); break;
                case MouseWheelEventArgs mwe when ReferenceEquals(routedEvent, MouseWheelEvent):
                    OnMouseWheel(mwe); break;
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
