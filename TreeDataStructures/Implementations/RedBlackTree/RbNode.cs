using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public enum RbColor : byte
{
    Red = 0,
    Black = 1
}

public class RbNode<TKey, TValue>(TKey key, TValue value)
    : Node<TKey, TValue, RbNode<TKey, TValue>>(key, value)
{
    public RbColor Color { get; set; } = RbColor.Red;
}

public static class RbNodeExtensions
{
    extension<TKey, TValue>(RbNode<TKey, TValue>? node)
    {
        public bool IsRed()
            => node is { Color: RbColor.Red };

        public bool IsBlack()
            => node is null || node.Color == RbColor.Black;
    }
}