using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null)
    : ITree<TKey, TValue> where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;

    public IComparer<TKey> Comparer { get; protected set; } =
        comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }

    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder()
        .Select(e => e.Key)
        .ToList();

    public ICollection<TValue> Values => InOrder()
        .Select(e => e.Value)
        .ToList();

    public virtual void Add(TKey key, TValue value)
    {
        var newNode = CreateNode(key, value);

        if (Root is null)
        {
            Root = newNode;
            Count++;
            OnNodeAdded(newNode);
            return;
        }

        var current = Root;
        while (true)
        {
            var cmp = Comparer.Compare(key, current.Key);

            if (cmp == 0)
            {
                current.Value = newNode.Value;
                return;
            }

            if (cmp > 0)
            {
                if (current.Right is null)
                {
                    current.Right = newNode;
                    newNode.Parent = current;
                    break;
                }

                current = current.Right;
            }
            else
            {
                if (current.Left is null)
                {
                    current.Left = newNode;
                    newNode.Parent = current;
                    break;
                }

                current = current.Left;
            }
        }

        Count++;
        OnNodeAdded(newNode);
    }

    public virtual bool Remove(TKey key)
    {
        var node = FindNode(key);
        if (node is null)
        {
            return false;
        }

        RemoveNode(node);
        Count--;
        return true;
    }

    protected virtual void RemoveNode(TNode node)
    {
        TNode? parent;
        TNode? child;

        if (node.Left is null)
        {
            parent = node.Parent;
            child = node.Right;
            Transplant(node, node.Right);
        }
        else if (node.Right is null)
        {
            parent = node.Parent;
            child = node.Left;
            Transplant(node, node.Left);
        }
        else
        {
            TNode candidate = node.Right;

            while (candidate.Left is not null) candidate = candidate.Left;

            if (candidate.Parent != node)
            {
                Transplant(candidate, candidate.Right);
                candidate.Right = node.Right;
                candidate.Right.Parent = candidate;
            }

            Transplant(node, candidate);
            candidate.Left = node.Left;
            candidate.Left.Parent = candidate;

            parent = candidate;
            child = candidate.Right;
        }

        OnNodeRemoved(parent, child);
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) is not null;

    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node is not null)
        {
            value = node.Value;
            return true;
        }

        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    #region Hooks

    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode)
    {
    }

    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child)
    {
    }

    /// <summary>
    /// Вызывается после доступа. 
    /// </summary>
    /// <param name="node">Узел, к которому получен доступ</param>
    protected virtual void OnNodeAccessed(TNode node)
    {
    }

    #endregion

    #region Helpers

    protected abstract TNode CreateNode(TKey key, TValue value);

    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current is not null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0)
            {
                OnNodeAccessed(current);
                return current;
            }

            current = cmp < 0 ? current.Left : current.Right;
        }

        return null;
    }

    protected void RotateLeft(TNode x)
    {
        if (x.Right is null) return;
        var y = x.Right;

        x.Right = y.Left;
        x.Right?.Parent = x;

        Transplant(x, y);

        y.Left = x;
        x.Parent = y;
    }

    protected void RotateRight(TNode y)
    {
        if (y.Left is null) return;
        var x = y.Left;

        y.Left = x.Right;
        x.Right?.Parent = y;

        Transplant(y, x);

        x.Right = y;
        y.Parent = x;
    }

    // LR case
    protected void RotateBigRight(TNode y)
    {
        if (y.Left is null) return;
        RotateLeft(y.Left);
        RotateRight(y);
    }

    // RL case
    protected void RotateBigLeft(TNode x)
    {
        if (x.Right is null) return;
        RotateRight(x.Right);
        RotateLeft(x);
    }

    // LL case
    protected void RotateDoubleLeft(TNode gparent)
    {
        var parent = gparent.Right;
        if (parent is null)
            return;

        RotateLeft(gparent);
        RotateLeft(parent);
    }

    // RR case
    protected void RotateDoubleRight(TNode gparent)
    {
        var parent = gparent.Left;
        if (parent is null)
            return;

        RotateRight(gparent);
        RotateRight(parent);
    }

    // подвешивает child (v) на место parent (u)
    // родитель 
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent is null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }

        v?.Parent = u.Parent;
    }

    #endregion


    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() =>
        new TreeIterator<TKey, TValue, TNode>(Root, TraversalStrategy.InOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() =>
        new TreeIterator<TKey, TValue, TNode>(Root, TraversalStrategy.PreOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() =>
        new TreeIterator<TKey, TValue, TNode>(Root, TraversalStrategy.PostOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() =>
        new TreeIterator<TKey, TValue, TNode>(Root, TraversalStrategy.InOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() =>
        new TreeIterator<TKey, TValue, TNode>(Root, TraversalStrategy.PreOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() =>
        new TreeIterator<TKey, TValue, TNode>(Root, TraversalStrategy.PostOrderReverse);


    public void Clear()
    {
        Root = null;
        Count = 0;
    }


    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        if (arrayIndex < 0 || arrayIndex > array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        if (array.Length - arrayIndex < Count)
            throw new ArgumentException("Destination is too small", nameof(array));

        foreach (var entry in InOrder())
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);


    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
        InOrder()
            .Select(e => new KeyValuePair<TKey, TValue>(e.Key, e.Value))
            .GetEnumerator();
}
