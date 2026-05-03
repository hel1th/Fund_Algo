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
            uint carry = 0;
            
            for (int j = 0; j < bDigs.Length; j++)
                carry = MultiplyHalf(aDigs[i], bDigs[j], result[i + j], carry, out result[i + j]);

            int k = i + bDigs.Length;
            while (carry != 0)
            {
                uint sum = result[k] + carry;
                carry = sum < result[k] ? 1u : 0u;
                result[k] = sum;
                k++;
            }
        }

        bool isNeg = a.IsNegative ^ b.IsNegative;
        return new BetterBigInteger(result, isNeg);
    }

    private static uint MultiplyHalf(uint a, uint b, uint acc, uint carryIn, out uint lo)
    {
        var aLo = a & 0xFFFF;
        var aHi = a >> 16;
        
        var bLo = b & 0xFFFF;
        var bHi = b >> 16;

        var ll = aLo * bLo;
        var lh = aLo * bHi;
        var hl = aHi * bLo;
        var hh = aHi * bHi;

        var mid = lh + hl;
        var midCarry = mid < lh ? 1u : 0u;

        var t0 = ll + (mid << 16);
        var c0 = t0 < ll ? 1u : 0u;

        var t1 = t0 + acc;
        var c1 = t1 < t0 ? 1u : 0u;

        var t2 = t1 + carryIn;
        var c2 = t2 < t1 ? 1u : 0u;

        lo = t2;

        return hh + (mid >> 16) + midCarry + c0 + c1 + c2;
    }
}
