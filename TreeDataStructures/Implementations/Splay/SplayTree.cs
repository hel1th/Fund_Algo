using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }

    protected override void RemoveNode(BstNode<TKey, TValue> node)
    {
        Splay(node);
        var (left, right) = Split(node);
        Merge(left, right);
    }

    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
    }

    protected override void OnNodeAccessed(BstNode<TKey, TValue> node)
    {
        Splay(node);
    }

    private void Splay(BstNode<TKey, TValue> node)
    {
        while (node.Parent is not null)
        {
            var parent = node.Parent;
            var gparent = parent.Parent;

            if (gparent is null)
            {
                if (node.IsLeftChild)

                    RotateRight(parent);
                else
                    RotateLeft(parent);
            }
            else
            {
                if (node.IsLeftChild && parent.IsLeftChild)
                    RotateDoubleRight(gparent);
                else if (node.IsLeftChild && parent.IsRightChild)
                    RotateBigLeft(gparent);
                else if (node.IsRightChild && parent.IsLeftChild)
                    RotateBigRight(gparent);
                else
                    RotateDoubleLeft(gparent);
            }
        }

        Root = node;
    }

    private (BstNode<TKey, TValue>? Left, BstNode<TKey, TValue>? Right) Split(BstNode<TKey, TValue> node)
    {
        var left = node.Left;
        var right = node.Right;

        left?.Parent = null;
        right?.Parent = null;

        return (left, right);
    }

    private void Merge(BstNode<TKey, TValue>? left, BstNode<TKey, TValue>? right)
    {
        if (left is null)
        {
            Root = right;
            return;
        }

        if (right is null)
        {
            Root = left;
            return;
        }

        var leftMax = GoRight(left);
        Splay(leftMax);
        leftMax.Right = right;
        right.Parent = leftMax;
        Root = leftMax;
    }
}
