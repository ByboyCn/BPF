using System;
using System.ComponentModel;
using Bpf.PropertySystem;

namespace Bpf.Data
{
    /// <summary>
    /// 编译式数据绑定(M6)。由源生成器从 {Binding Path} 生成,持有编译期的 get/set 访问器 lambda。
    /// 与反射式 <see cref="Binding"/> 对应,但零反射、AOT 完全兼容。
    ///
    /// 设计:源生成器为每个 {Binding} 生成一个 CompiledBinding 实例,
    /// 把 vm =&gt; vm.Path 和 (vm, v) =&gt; vm.Path = v 编译成直接调用。
    /// 运行时只持有委托,沿路径取值/赋值无任何反射。
    /// </summary>
    public sealed class CompiledBinding
    {
        /// <summary>从源对象读值(路径已编译进委托)。null = 路径不可读。</summary>
        public Func<object?, object?>? Get { get; }

        /// <summary>向源对象写值(路径已编译进委托,TwoWay 用)。null = 路径只读。</summary>
        public Action<object?, object?>? Set { get; }

        /// <summary>绑定方向。</summary>
        public BindingMode Mode { get; }

        /// <summary>值转换器(源→目标)。null = 不转换。</summary>
        public IValueConverter? Converter { get; }

        /// <summary>转换器参数。</summary>
        public object? ConverterParameter { get; }

        /// <summary>
        /// 源属性的最后一个字段的名称,用于订阅 INotifyPropertyChanged。
        /// 源生成器根据路径推出(如 "Person.Name" → 监听 Name 变更,以及路径上每段的变更)。
        /// 为简化:M6 只监听路径最后一段的属性名变更 + 路径上各中间对象的 INPC。
        /// </summary>
        public string[] ObservedPropertyNames { get; }

        public CompiledBinding(
            Func<object?, object?>? get,
            Action<object?, object?>? set,
            BindingMode mode,
            string[] observedPropertyNames,
            IValueConverter? converter = null,
            object? converterParameter = null)
        {
            Get = get;
            Set = set;
            Mode = mode;
            Converter = converter;
            ConverterParameter = converterParameter;
            ObservedPropertyNames = observedPropertyNames ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// CompiledBinding 的运行时实例,挂接到目标控件的属性 + 源对象的 INPC。
    /// 内部类(同 BindingExpression),由 Control.SetCompiledBinding 创建。
    /// </summary>
    internal sealed class CompiledBindingExpression
    {
        private readonly Bpf.Controls.Control _target;
        private readonly BpfProperty _property;
        private readonly CompiledBinding _binding;
        private object? _source;
        private bool _isUpdating;

        public CompiledBindingExpression(Bpf.Controls.Control target, BpfProperty property, CompiledBinding binding)
        {
            _target = target;
            _property = property;
            _binding = binding;
        }

        /// <summary>挂接:订阅源的 INPC,首次取值刷新目标。</summary>
        public void Attach(object? source)
        {
            Rebind(source);
        }

        /// <summary>换源(DataContext 变化时):解订旧源、订新源、刷新。</summary>
        public void Rebind(object? newSource)
        {
            // 解订旧
            if (_source is INotifyPropertyChanged oldNpc)
                oldNpc.PropertyChanged -= OnSourcePropertyChanged;

            _source = newSource;

            if (_source is INotifyPropertyChanged npc)
                npc.PropertyChanged += OnSourcePropertyChanged;

            Refresh();
        }

        public void Detach()
        {
            if (_source is INotifyPropertyChanged npc)
                npc.PropertyChanged -= OnSourcePropertyChanged;
            _source = null;
        }

        /// <summary>从源读值,经转换器,写入目标属性。</summary>
        public void Refresh()
        {
            if (_binding.Get is null) return;
            if (_isUpdating) return;
            _isUpdating = true;
            try
            {
                object? raw = _binding.Get(_source);
                object? converted = _binding.Converter is not null
                    ? _binding.Converter.Convert(raw, _property.PropertyType, _binding.ConverterParameter)
                    : raw;
                WriteToTarget(converted);
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>源属性变更回调:检查是否是观察的属性,是则刷新。</summary>
        private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_binding.ObservedPropertyNames is null || _binding.ObservedPropertyNames.Length == 0)
            {
                Refresh();
                return;
            }
            // 任一观察名匹配即刷新(M6 简化:不区分路径深度)
            foreach (var name in _binding.ObservedPropertyNames)
            {
                if (e.PropertyName == name) { Refresh(); return; }
            }
        }

        /// <summary>把值写入目标控件的属性(用非泛型入口,绕过绑定回查)。</summary>
        private void WriteToTarget(object? value)
        {
            // SetValueInternal 是非泛型的(BpfProperty, object?),内部 PropertyValueStore
            // 用 object[] 存储,无需反射。这正好满足编译式绑定的非泛型写入需求。
            _target.SetValueInternal(_property, value);
        }
    }
}
