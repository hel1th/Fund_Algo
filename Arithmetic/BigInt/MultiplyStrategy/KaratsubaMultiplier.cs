using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    private readonly int _threshold; // ниже порога — SimpleMultiplier
    private readonly SimpleMultiplier _simple = new();

    public KaratsubaMultiplier(int treshold = 32)
    {
        if (treshold < 1)
            throw new ArgumentException("Threshold must be at least 1", nameof(treshold));
        
        _threshold = treshold;
    }

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        bool isNeg = a.IsNegative ^ b.IsNegative;
        var result = MultiplyMagnitude(a.GetDigits().ToArray(), b.GetDigits().ToArray());
        return new BetterBigInteger(result, isNeg);
    }

    private uint[] MultiplyMagnitude(uint[] a, uint[] b)
    {
        // Базовый случай — делегируем SimpleMultiplier
        if (a.Length <= _threshold || b.Length <= _threshold)
        {
            var aB = new BetterBigInteger(a);
            var bB = new BetterBigInteger(b);
            return _simple.Multiply(aB, bB).GetDigits().ToArray();
        }

        int m = Math.Max(a.Length, b.Length) / 2;

        
        // take low and high with median split at m
        var aLo = Slice(a, 0, m);
        var aHi = Slice(a, m, a.Length - m);
        var bLo = Slice(b, 0, m);
        var bHi = Slice(b, m, b.Length - m);

        var z0 = MultiplyMagnitude(aLo, bLo);
        var z2 = MultiplyMagnitude(aHi, bHi);

        // z1 = (aLo + aHi) * (bLo + bHi) - z0 - z2
        var aSum = MagnitudeHelper.Add(aLo, aHi);
        var bSum = MagnitudeHelper.Add(bLo, bHi);
        var z1Raw = MultiplyMagnitude(aSum, bSum);
        var z1 = MagnitudeHelper.Subtract(MagnitudeHelper.Subtract(z1Raw, z0), z2);

        var z1Shifted = ShiftLeft(z1, m);
        var z2Shifted = ShiftLeft(z2, 2 * m);

        // result = z0 + z1 * B^m + z2 * B^2m (2m is basically 
        return MagnitudeHelper.Add(MagnitudeHelper.Add(z0, z1Shifted), z2Shifted);
    }

    private static uint[] Slice(uint[] a, int offset, int length)
    {
        if (offset >= a.Length) return [0];

        length = Math.Min(length, a.Length - offset);

        var result = new uint[length];
        Array.Copy(a, offset, result, 0, length);

        return MagnitudeHelper.Trim(result);
    }

    private static uint[] ShiftLeft(uint[] a, int limbCount)
    {
        if (a is [0]) return [0];

        var result = new uint[a.Length + limbCount];
        Array.Copy(a, 0, result, limbCount, a.Length);

        return MagnitudeHelper.Trim(result);
    }


}