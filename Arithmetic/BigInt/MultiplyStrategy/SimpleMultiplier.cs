using System;
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

        bool isNeg = a.IsNegative ^ b.IsNegative;
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

        // distribute the middle
        var midLo = mid << 16;
        var midHi = (mid >> 16) + (carryMid << 16);

        var s0 = p0 + midLo;
        var c0 = s0 < p0 ? 1u : 0u;

        var hi = p3 + midHi + c0;

        var s1 = s0 + acc;
        var c1 = s1 < s0 ? 1u : 0u;

        var s2 = s1 + carryIn;
        var c2 = s2 < s1 ? 1u : 0u;

        hi = hi + c1 + c2;
        lo = s2;

        return hi;
    }
}