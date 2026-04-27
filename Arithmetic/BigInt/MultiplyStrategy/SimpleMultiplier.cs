using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        var aDigs = a.GetDigits();
        var bDigs = b.GetDigits();

        var result = new uint[aDigs.Length + bDigs.Length];

        for (int i = 0; i < aDigs.Length; i++)
        {
            ulong carry = 0;
            for (int j = 0; j < bDigs.Length; j++)
            {
                // gauranteed no overflow
                ulong curr = (ulong)aDigs[i] * bDigs[j] + result[i + j] + carry;
                result[i + j] = (uint)curr;
                carry = curr >> 32;
            }

            result[i + bDigs.Length] += (uint)carry;
        }
        
        bool isNeg = a.IsNegative ^ b.IsNegative;
        return new BetterBigInteger(result, isNeg);
    }
}