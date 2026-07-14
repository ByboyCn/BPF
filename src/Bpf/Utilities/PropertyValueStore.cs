using System;
using Bpf.PropertySystem;

namespace Bpf.Utilities
{
    /// <summary>
    /// 高频路径:控件属性值的存储与查询。采用稀疏数组 + property id 索引,
    /// 避免每次访问属性都 boxing/分配 Dictionary。
    /// </summary>
    /// <remarks>
    /// 设计:
    /// - 大多数控件只设置少量几个属性,故采用小型数组;
    /// - 用 property.Id 直接索引需要 O(N) 空间,这里改用简单的线性查找(N 极小);
    /// - GetValue 未命中时返回 property 提供的默认值(由调用方提供,避免泛型约束)。
    ///
    /// AOT 友好性:仅使用 object[] 存储,无反射、无 Emit。
    /// </remarks>
    internal sealed class PropertyValueStore
    {
        // 简化实现:用两个并行数组(id + value),N 通常 ≤ 16,线性查找足够。
        private int[]? _ids;
        private object?[]? _values;
        private int _count;

        public bool IsEmpty => _count == 0;

        /// <summary>
        /// 读取。未命中返回 false,调用方应回落到属性默认值。
        /// </summary>
        public bool TryGetValue(BpfProperty property, out object? value)
        {
            var id = property.Id;
            var ids = _ids;
            if (ids is not null)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (ids[i] == id)
                    {
                        value = _values![i];
                        return true;
                    }
                }
            }
            value = null;
            return false;
        }

        /// <summary>
        /// 写入。返回是否为"值变更"(用于触发失效通知)。
        /// </summary>
        public bool SetValue(BpfProperty property, object? value, out object? oldValue)
        {
            var id = property.Id;
            var ids = _ids;
            if (ids is not null)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (ids[i] == id)
                    {
                        oldValue = _values![i];
                        if (!Equals(oldValue, value))
                        {
                            _values[i] = value;
                            return true;
                        }
                        return false;
                    }
                }
            }

            // 新增槽位
            if (_ids is null || _count >= _ids.Length)
            {
                Grow();
            }

            _ids![_count] = id;
            _values![_count] = value;
            _count++;
            oldValue = null;
            return true;
        }

        /// <summary>清空。</summary>
        public void Clear()
        {
            if (_values is not null)
            {
                Array.Clear(_values, 0, _count);
            }
            _count = 0;
        }

        private void Grow()
        {
            var newCapacity = _ids is null ? 4 : _ids.Length * 2;
            var newIds = new int[newCapacity];
            var newValues = new object?[newCapacity];
            if (_ids is not null)
            {
                Array.Copy(_ids, newIds, _count);
                Array.Copy(_values!, newValues, _count);
            }
            _ids = newIds;
            _values = newValues;
        }
    }
}
