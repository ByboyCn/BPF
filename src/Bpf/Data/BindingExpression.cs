using System;
using Bpf.Controls;
using Bpf.PropertySystem;

namespace Bpf.Data
{
    /// <summary>
    /// 绑定的运行时实例。挂在控件的 StyledProperty 上:
    /// - 监听 Source 的 INotifyPropertyChanged,变化时刷新目标属性
    /// - TwoWay 模式下,目标属性变化时反向写入 Source
    /// </summary>
    internal sealed class BindingExpression
    {
        private readonly Control _target;
        private readonly BpfProperty _property;
        private readonly Binding _binding;
        private bool _isUpdating; // 防重入(TwoWay 循环)

        public BindingExpression(Control target, BpfProperty property, Binding binding)
        {
            _target = target;
            _property = property;
            _binding = binding;
        }

        /// <summary>启动绑定:订阅 Source 变化通知 + 首次刷新。</summary>
        public void Attach()
        {
            if (_binding.Source is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged += OnSourcePropertyChanged;
            }
            Refresh();
        }

        /// <summary>停止绑定:取消订阅。</summary>
        public void Detach()
        {
            if (_binding.Source is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged -= OnSourcePropertyChanged;
            }
        }

        /// <summary>从 Source 读取最新值,写入目标控件属性。</summary>
        public void Refresh()
        {
            var value = _binding.Evaluate();
            if (value is null) return;

            _isUpdating = true;
            try
            {
                // 用 SetValueInternal 直接写本地值,不走绑定回查(防递归)
                _target.SetValueInternal(_property, value);
                _target.RequestRedraw();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>目标属性被外部改变(如 TextBox 输入),反向写回 Source(TwoWay)。</summary>
        public void PushBack(object? newValue)
        {
            if (_isUpdating) return; // 防重入
            if (_binding.Mode != BindingMode.TwoWay) return;
            _binding.SetValue(newValue);
        }

        private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 只处理路径中涉及的属性名(简化:检查路径最后一段)
            var lastSegment = _binding.Path;
            var dotIdx = lastSegment.LastIndexOf('.');
            if (dotIdx >= 0) lastSegment = lastSegment.Substring(dotIdx + 1);

            // 空属性名(全量刷新)或匹配路径
            if (string.IsNullOrEmpty(e.PropertyName) ||
                string.Equals(e.PropertyName, lastSegment, StringComparison.Ordinal))
            {
                Refresh();
            }
        }
    }
}
