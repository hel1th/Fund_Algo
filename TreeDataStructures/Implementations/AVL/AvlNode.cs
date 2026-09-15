using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlNode<TKey, TValue>(TKey key, TValue value)
    : Node<TKey, TValue, AvlNode<TKey, TValue>>(key, value)
{
    public int Height { get; set; } = 1;
}