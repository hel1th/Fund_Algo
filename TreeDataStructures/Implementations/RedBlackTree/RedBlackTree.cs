using TreeDataStructures.Core;
using static TreeDataStructures.Implementations.RedBlackTree.RbColor;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value) => new RbNode<TKey, TValue>(key, value);

    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        RbFixInsert(newNode);
    }

    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        RbFixRemove(child, parent);
    }

    private void RbFixRemove(RbNode<TKey, TValue>? node, RbNode<TKey, TValue>? parent)
    {
        // node is root -> recolor to black END
        if (node == Root)
        {
            node!.Color = Black;
            return;
        }

        // node is null -> use parent to find sibling
        parent = node?.Parent ?? parent;
        if (parent == null) return;

        if (node == null || node.IsLeftChild)
        {
            var sibling = parent.Right!;
            if (sibling == null)
            {
                RbFixRemove(parent, parent.Parent);
                return;
            }
            
            // sibling is red -> rotate left parent and recolor
            if (sibling.IsRed())
            {
                sibling.Color = Black;
                parent.Color = Red;
                RotateLeft(parent);
                RbFixRemove(node, parent);
                return;
            }

            // 2 black nephews
            if (sibling.Left.IsBlack() && sibling.Right.IsBlack())
            {
                sibling.Color = Red;
                if (parent.IsBlack())
                    RbFixRemove(parent, parent.Parent);
                else
                    parent.Color = Black;
                return;
            }

            // far nephew is black -> rotate right sibling (case 5)
            if (sibling.Right.IsBlack())
            {
                sibling.Left?.Color = Black;
                sibling.Color = Red;
                RotateRight(sibling);
                sibling = parent.Right!;
            }

            // far nephew is red -> rotate left parent (case 6)
            sibling.Color = parent.Color;
            parent.Color = Black;
            sibling.Right!.Color = Black;
            RotateLeft(parent);
        }
        else
        {
            var sibling = parent.Left!;
            if (sibling == null)
            {
                RbFixRemove(parent, parent.Parent);
                return;
            }
            // sibling is red -> rotate right parent and recolor
            if (sibling.IsRed())
            {
                sibling.Color = Black;
                parent.Color = Red;
                RotateRight(parent);
                RbFixRemove(node, parent);
                return;
            }

            // 2 black nephews
            if (sibling.Left.IsBlack() && sibling.Right.IsBlack())
            {
                sibling.Color = Red;
                if (parent.IsBlack())
                    RbFixRemove(parent, parent.Parent);
                else
                    parent.Color = Black;
                return;
            }

            // far nephew is black -> rotate left sibling (case 5)
            if (sibling.Left.IsBlack())
            {
                sibling.Right?.Color = Black;
                sibling.Color = Red;
                RotateLeft(sibling);
                sibling = parent.Left!;
            }

            // far nephew is red -> rotate right parent (case 6)
            sibling.Color = parent.Color;
            parent.Color = Black;
            sibling.Left!.Color = Black;
            RotateRight(parent);
        }
    }

    private void RbFixInsert(RbNode<TKey, TValue> node)
    {
        // no violation if node is black
        if (node.IsBlack())
            return;

        // node is the root
        if (node.Parent is null)
        {
            node.Color = Black;
            return;
        }

        var parent = node.Parent;
        // parent is the root
        if (parent.Parent is null)
        {
            parent.Color = Black;
            return;
        }

        // double red 
        var grandparent = parent.Parent;

        var uncle = grandparent.Left == parent ? grandparent.Right! : grandparent.Left!;

        // uncle is red -> recolor and move up 
        if (uncle.IsRed())
        {
            parent.Color = Black;
            uncle.Color = Black;
            grandparent.Color = Red;

            RbFixInsert(grandparent);
        }
        // uncle is black
        else
        {
            if (parent.IsLeftChild)
            {
                // LR case rotate left parent; 
                if (node.IsRightChild)
                {
                    RotateLeft(parent);
                    parent = node; // node moved up (parent's old place)
                }

                // LL case rotate right grandparent; par = black, gpar = red
                RotateRight(grandparent);
                parent.Color = Black;
                grandparent.Color = Red;
            }
            else
            {
                // RL case rotate right parent;
                if (node.IsLeftChild)
                {
                    RotateRight(parent);
                    parent = node; // node moved up (parent's old place)
                }

                // RR case rotate left grandparent; par = black, gpar = red
                RotateLeft(grandparent);
                parent.Color = Black;
                grandparent.Color = Red;
            }
        }
    }
}