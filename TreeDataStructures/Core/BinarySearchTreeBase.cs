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

    protected void RotateBigLeft(TNode x)
    {
        var rightChild = x.Right;
        if (rightChild == null)
            return;

        RotateRight(rightChild);
        RotateLeft(x);
    }

    protected void RotateBigRight(TNode y)
    {
        var leftChild = y.Left;
        if (leftChild == null)
            return;

        RotateLeft(leftChild);
        RotateRight(y);
    }

    protected void RotateDoubleLeft(TNode x)
    {
        var rightChild = x.Right;
        if (rightChild == null)
            return;

        RotateLeft(rightChild);
        RotateLeft(x);
    }

    protected void RotateDoubleRight(TNode y)
    {
        var leftChild = y.Left;
        if (leftChild == null)
            return;

        RotateRight(leftChild);
        RotateRight(y);
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

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var entry in InOrder())
            yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() => new TreeIterator(Root, TraversalStrategy.InOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() => new TreeIterator(Root, TraversalStrategy.PostOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() =>
        new TreeIterator(Root, TraversalStrategy.InOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() =>
        new TreeIterator(Root, TraversalStrategy.PreOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() =>
        new TreeIterator(Root, TraversalStrategy.PostOrderReverse);

    private enum TraversalStrategy
    {
        InOrder,
        PreOrder,
        PostOrder,
        InOrderReverse,
        PreOrderReverse,
        PostOrderReverse
    }

    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator :
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TNode? _root;
        private readonly TraversalStrategy _strategy;

        private TNode? _current;
        private bool _started;


        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _started = false;
            _current = null;
        }

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;

        public TreeEntry<TKey, TValue> Current =>
            _current is not null
                ? new TreeEntry<TKey, TValue>(_current.Key, _current.Value, 0)
                : throw new InvalidOperationException();

        object IEnumerator.Current => Current;


        public bool MoveNext()
        {
            if (!_started)
            {
                _current = GetFirst(_root, _strategy);
                _started = true;
            }
            else
            {
                _current = GetNext(_current, _strategy);
            }

            return _current is not null;
        }

        public void Reset()
        {
            _current = null;
            _started = false;
        }


        public void Dispose()
        {
            // TODO release managed resources here
        }

        private static TNode? GetFirst(TNode? root, TraversalStrategy strategy)
        {
            if (root is null) return null;

            return strategy switch
            {
                TraversalStrategy.InOrder => GoLeft(root),
                TraversalStrategy.InOrderReverse => GoRight(root),

                TraversalStrategy.PreOrder => root,
                TraversalStrategy.PreOrderReverse => GoDeepRight(root),

                TraversalStrategy.PostOrder => GoDeepLeft(root),
                TraversalStrategy.PostOrderReverse => root,

                _ => throw new ArgumentOutOfRangeException(nameof(strategy))
            };
        }

        private static TNode? GetNext(TNode? node, TraversalStrategy strategy)
        {
            if (node is null) return null;

            return strategy switch
            {
                TraversalStrategy.InOrder => NextInOrder(node),
                TraversalStrategy.InOrderReverse => NextInOrderReverse(node),
                TraversalStrategy.PreOrder => NextPreOrder(node),
                TraversalStrategy.PreOrderReverse => NextPreOrderReverse(node),
                TraversalStrategy.PostOrder => NextPostOrder(node),
                TraversalStrategy.PostOrderReverse => NextPostOrderReverse(node),
                _ => throw new ArgumentOutOfRangeException(nameof(strategy))
            };
        }

        private static TNode? NextInOrder(TNode node)
        {
            if (node.Right is not null)
                return GoLeft(node.Right);

            var cur = node;
            while (cur.Parent is not null && cur.IsRightChild)
                cur = cur.Parent;

            return cur.Parent;
        }

        private static TNode? NextInOrderReverse(TNode node)
        {
            if (node.Left is not null)
                return GoRight(node.Left);

            var cur = node;
            while (cur.Parent is not null && cur.IsLeftChild)
                cur = cur.Parent;

            return cur.Parent;
        }

        private static TNode? NextPreOrder(TNode node)
        {
            if (node.Left is not null) return node.Left;
            if (node.Right is not null) return node.Right;

            var cur = node;
            while (cur.Parent is not null)
            {
                if (cur.IsLeftChild && cur.Parent.Right is not null)
                    return cur.Parent.Right;
                cur = cur.Parent;
            }

            return null;
        }

        private static TNode? NextPreOrderReverse(TNode node)
        {
            if (node.Parent is null)
                return null;

            if (node.IsRightChild && node.Parent.Left is not null)
            {
                var candidate = node.Parent.Left;
                return GoDeepRight(candidate);
            }

            return node.Parent;
        }


        private static TNode? NextPostOrder(TNode node)
        {
            if (node.Parent is null) return null;

            if (node.IsLeftChild && node.Parent.Right is not null)
                return GoDeepLeft(node.Parent.Right);

            return node.Parent;
        }

        private static TNode? NextPostOrderReverse(TNode node)
        {
            if (node.Right is not null) return node.Right;
            if (node.Left is not null) return node.Left;

            while (node.Parent is not null)
            {
                var parent = node.Parent;

                if (parent.Left is not null && parent.Right == node)
                    return parent.Left;

                node = parent;
            }

            return null;
        }
    }

    protected static TNode GoLeft(TNode node)
    {
        while (node.Left is not null) node = node.Left;
        return node;
    }

    protected static TNode GoRight(TNode node)
    {
        while (node.Right is not null) node = node.Right;
        return node;
    }

    protected static TNode GoDeepLeft(TNode node)
    {
        while (node.Left is not null || node.Right is not null)
            node = node.Left ?? node.Right!;
        return node;
    }

    protected static TNode GoDeepRight(TNode node)
    {
        while (node.Right is not null || node.Left is not null)
            node = node.Right ?? node.Left!;
        return node;
    }

    public void Clear()
    {
        Root = null;
        Count = 0;
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotImplementedException();
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}
