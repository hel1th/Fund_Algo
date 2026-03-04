using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        // No further rebalancing needed if already done once
        FixTreeStructure(newNode, stopAfterRebalance: true);
    }


    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        FixTreeStructure(parent);
    }

    protected void FixTreeStructure(AvlNode<TKey, TValue>? node, bool stopAfterRebalance = false)
    {
        while (node is not null)
        {
            UpdateHeight(node);
            if (!IsBalanced(node))
            {
                Rebalance(node);
                if (stopAfterRebalance)
                    break;
            }

            node = node.Parent;
        }
    }

    private static int Height(AvlNode<TKey, TValue>? node) => node?.Height ?? 0;

    // negative => right heavy, positive => left heavy
    protected int BalanceFactor(AvlNode<TKey, TValue> node)
        => Height(node.Left) - Height(node.Right);

    protected bool IsBalanced(AvlNode<TKey, TValue> node)
        => BalanceFactor(node) is <= 1 and >= -1;

    protected void UpdateHeight(AvlNode<TKey, TValue>? node)
    {
        node?.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
    }


    protected void Rebalance(AvlNode<TKey, TValue> node)
    {
        UpdateHeight(node);
        var balanceFactor = BalanceFactor(node);

        AvlNode<TKey, TValue>? newRoot = null;
        switch (balanceFactor)
        {
            case > 1:
                if (BalanceFactor(node.Left!) < 0)
                    RotateLeft(node.Left!);

                newRoot = node.Left!;
                RotateRight(node);

                break;

            case < -1:
                if (BalanceFactor(node.Right!) > 0)
                    RotateRight(node.Right!);

                newRoot = node.Right!;
                RotateLeft(node);
                break;
        }

        UpdateHeight(node);
        UpdateHeight(newRoot);
    }
}