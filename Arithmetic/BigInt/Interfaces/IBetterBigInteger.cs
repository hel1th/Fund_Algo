namespace Arithmetic.BigInt.Interfaces;


public interface IBigInteger : IComparable<IBigInteger>, IEquatable<IBigInteger>
{
    bool IsNegative { get; }
    ReadOnlySpan<uint> GetDigits(); // Little-endian представление
    string ToString(int radix);
}