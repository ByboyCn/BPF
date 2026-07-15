using System;
using System.Collections;
using System.Collections.Generic;

namespace Bpf.Data
{
    /// <summary>集合变更动作。</summary>
    public enum NotifyCollectionChangedAction
    {
        Add,
        Remove,
        Replace,
        Move,
        Reset,
    }

    /// <summary>集合变更事件参数。</summary>
    public sealed class NotifyCollectionChangedEventArgs : EventArgs
    {
        public NotifyCollectionChangedAction Action { get; }
        public IList? NewItems { get; }
        public IList? OldItems { get; }
        public int NewStartingIndex { get; }
        public int OldStartingIndex { get; }

        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action)
        {
            Action = action;
            NewStartingIndex = -1;
            OldStartingIndex = -1;
        }

        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object? item, int index)
        {
            Action = action;
            NewItems = action == NotifyCollectionChangedAction.Remove ? null : new[] { item };
            OldItems = action == NotifyCollectionChangedAction.Remove ? new[] { item } : null;
            NewStartingIndex = index;
            OldStartingIndex = index;
        }
    }

    public delegate void NotifyCollectionChangedEventHandler(object? sender, NotifyCollectionChangedEventArgs e);

    /// <summary>
    /// 可观察集合:Add/Remove 时触发 CollectionChanged 事件。
    /// ListBox 的 ItemsSource 用它实现动态更新。
    /// </summary>
    public sealed class ObservableCollection<T> : IList<T>, IList, INotifyCollectionChanged
    {
        private readonly List<T> _items = new List<T>();

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        // ── IList<T> ──

        public T this[int index]
        {
            get => _items[index];
            set
            {
                var old = _items[index];
                _items[index] = value;
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public int Count => _items.Count;
        public bool IsReadOnly => false;

        public void Add(T item)
        {
            _items.Add(item);
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _items.Count - 1));
        }

        public bool Remove(T item)
        {
            int idx = _items.IndexOf(item);
            if (idx < 0) return false;
            _items.RemoveAt(idx);
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, idx));
            return true;
        }

        public void RemoveAt(int index)
        {
            var item = _items[index];
            _items.RemoveAt(index);
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        public void Clear()
        {
            _items.Clear();
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Insert(int index, T item)
        {
            _items.Insert(index, item);
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public int IndexOf(T item) => _items.IndexOf(item);
        public bool Contains(T item) => _items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        // ── IList(非泛型,支持 IEnumerable 绑定) ──

        object? IList.this[int index]
        {
            get => _items[index];
            set => this[index] = (T)value!;
        }

        bool IList.IsFixedSize => false;
        bool ICollection.IsSynchronized => false;
        object? ICollection.SyncRoot => this;

        int IList.Add(object? value) { Add((T)value!); return _items.Count - 1; }
        bool IList.Contains(object? value) => value is T t && Contains(t);
        int IList.IndexOf(object? value) => value is T t ? IndexOf(t) : -1;
        void IList.Insert(int index, object? value) => Insert(index, (T)value!);
        void IList.Remove(object? value) { if (value is T t) Remove(t); }
        void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
    }

    /// <summary>可观察集合接口(ListBox 用)。</summary>
    public interface INotifyCollectionChanged
    {
        event NotifyCollectionChangedEventHandler? CollectionChanged;
    }
}
