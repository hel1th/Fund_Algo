using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        var aDigs = a.GetDigits();
        var bDigs = b.GetDigits();
        var result = new uint[aDigs.Length + bDigs.Length];

        // Schoolbook O(N^2) multiplication
        for (var i = 0; i < aDigs.Length; i++)
        {
            var aVal = aDigs[i];

            // Optimization: Skip loop if the multiplier limb is 0
            if (aVal == 0) continue;

            uint carry = 0;
            for (var j = 0; j < bDigs.Length; j++)
            {
                var bVal = bDigs[j];

                // result[i + j] acts as the ongoing accumulator
                carry = MultiplyHalf(aVal, bVal, result[i + j], carry, out var lo);
                result[i + j] = lo;
            }

            result[i + bDigs.Length] = carry;
        }

        var isNeg = a.IsNegative ^ b.IsNegative;
        return new BetterBigInteger(result, isNeg);
    }

    private static uint MultiplyHalf(uint a, uint b, uint acc, uint carryIn, out uint lo)
    {
        var aLo = a & 0xFFFFu;
        var aHi = a >> 16;
        var bLo = b & 0xFFFFu;
        var bHi = b >> 16;

        var p0 = aLo * bLo;
        var p1 = aHi * bLo;
        var p2 = aLo * bHi;
        var p3 = aHi * bHi;

        var mid = p1 + p2;
        var carryMid = mid < p1 ? 1u : 0u;

        var midLo = mid << 16;
        var midHi = (mid >> 16) | (carryMid << 16);

        // lo part
        var s0 = p0 + midLo;
        var c0 = s0 < p0 ? 1u : 0u;

        // hi part — каждое сложение отдельно с carry
        var hi = p3;

        var t0 = hi + midHi;
        var ct0 = t0 < hi ? 1u : 0u;
        hi = t0;

        var t1 = hi + c0;
        var ct1 = t1 < hi ? 1u : 0u;
        hi = t1;

        // добавляем acc и carryIn в lo, пробрасываем carries в hi
        var s1 = s0 + acc;
        var c1 = s1 < s0 ? 1u : 0u;

        var s2 = s1 + carryIn;
        var c2 = s2 < s1 ? 1u : 0u;

        var t2 = hi + c1;
        var ct2 = t2 < hi ? 1u : 0u;
        hi = t2;

        var t3 = hi + c2;
        var ct3 = t3 < hi ? 1u : 0u;
        hi = t3;

        // carries из hi не могут переполнить второй uint —
        // максимальное hi = 0xFFFFFFFF, ct* суммарно ≤ 4, overflow невозможен
        hi = hi + ct0 + ct1 + ct2 + ct3;

        lo = s2;
        return hi;
    }
}