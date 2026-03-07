using System.Collections;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public class TreeIterator<TKey, TValue, TNode>(TNode? root, TraversalStrategy strategy)
    : IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    where TNode : Node<TKey, TValue, TNode>
{
    private TNode? _current;
    private bool _started;

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
            _current = GetFirst(root, strategy);
            _started = true;
        }
        else
        {
            _current = GetNext(_current, strategy);
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
            return GoDeepRight(node.Parent.Left);

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

    public static TNode GoLeft(TNode node)
    {
        while (node.Left is not null) node = node.Left;
        return node;
    }

    public static TNode GoRight(TNode node)
    {
        while (node.Right is not null) node = node.Right;
        return node;
    }

    public static TNode GoDeepLeft(TNode node)
    {
        while (node.Left is not null || node.Right is not null)
            node = node.Left ?? node.Right!;
        return node;
    }

    public static TNode GoDeepRight(TNode node)
    {
        while (node.Right is not null || node.Left is not null)
            node = node.Right ?? node.Left!;
        return node;
    }
}
