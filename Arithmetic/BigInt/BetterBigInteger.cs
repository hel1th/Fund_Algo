using System.Text;
using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit;

    private uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data;

    public bool IsNegative => _signBit == 1;

    /// От массива цифр (little endian)
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        _signBit = isNegative ? 1 : 0;

        var normLen = digits.Length;

        while (normLen > 0 && digits[normLen - 1] == 0)
            normLen--;

        switch (normLen)
        {
            case 0:
                _smallValue = 0;
                return;
            case 1:
                _smallValue = digits[0];
                return;
        }

        _data = new uint[normLen];
        Array.Copy(digits, _data, normLen);
    }

    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false) : this(digits.ToArray(), isNegative)
    {
    }

    // BetterBigInteger("10011", 2)
    // BetterBigInteger("64616", 10)
    // BetterBigInteger(    "0", 10)
    // BetterBigInteger(   "-0", 10)
    public BetterBigInteger(string value, int radix)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Value cannot be null or empty", nameof(value));
        if (radix < 2 || radix > 36)
            throw new ArgumentOutOfRangeException(nameof(radix));

        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        var start = 0;
        var isNeg = true;
        if (value[0] == '-')
        {
            start++;
            isNeg = true;
        }

        var result = new BetterBigInteger(Array.Empty<uint>());
        var radixBig = new BetterBigInteger([(uint)radix]);

        for (int i = start; i < value.Length; i++)
        {
            var c = char.ToUpperInvariant(value[i]);
            var digit = chars.IndexOf(c);
            if (digit < 0 || digit >= radix)
                throw new FormatException($"Invalid character '{c}' for radix {radix}");

            result = result * radixBig + new BetterBigInteger([(uint)digit]);
        }

        _data = result._data;
        _smallValue = result._smallValue;
        _signBit = isNeg && result.GetDigits() is not [0] ? 1 : 0;
    }


    public ReadOnlySpan<uint> GetDigits() => _data ?? [_smallValue];

    public int CompareTo(IBigInteger? other)
    {
        if (other is null) return 1;

        if (IsNegative != other.IsNegative)
            return IsNegative ? -1 : 1;

        var cmp = CompareMagnitude(GetDigits(), other.GetDigits());
        return IsNegative ? -cmp : cmp;
    }

    private static int CompareMagnitude(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        if (a.Length != b.Length)
            return a.Length > b.Length ? 1 : -1;

        for (var i = a.Length - 1; i >= 0; i--)
        {
            if (a[i] != b[i])
                return a[i] > b[i] ? 1 : -1;
        }

        return 0;
    }

    public bool Equals(IBigInteger? other) => CompareTo(other) == 0;
    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(IsNegative);
        foreach (var limb in GetDigits())
            hash.Add(limb);
        return hash.ToHashCode();
    }

    private static uint[] AddMagnitudes(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        var result = new uint[Math.Max(a.Length, b.Length) + 1];
        ulong carry = 0;

        for (var i = 0; i < result.Length - 1; i++)
        {
            ulong aVal = i < a.Length ? a[i] : 0UL;
            ulong bVal = i < b.Length ? b[i] : 0UL;

            var currSum = aVal + bVal + carry;

            result[i] = (uint)currSum;
            carry = currSum >> 32;
        }

        result[^1] = (uint)carry;
        return result;
    }


    // a >= b is guaranteed by calling code
    private static uint[] SubtractMagnitudes(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        var result = new uint[a.Length];
        long borrow = 0;

        for (var i = 0; i < a.Length; i++)
        {
            long aVal = a[i];
            var bVal = i < b.Length ? b[i] : 0L;

            var diff = aVal - bVal - borrow;


            result[i] = (uint)diff;
            borrow = diff < 0 ? 1L : 0L;
        }

        return result;
    }

    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        var aDigs = a.GetDigits();
        var bDigs = b.GetDigits();

        if (a.IsNegative == b.IsNegative)
            return new BetterBigInteger(AddMagnitudes(aDigs, bDigs), a.IsNegative);

        return CompareMagnitude(aDigs, bDigs) switch
        {
            1 => new BetterBigInteger(SubtractMagnitudes(aDigs, bDigs), a.IsNegative),
            -1 => new BetterBigInteger(SubtractMagnitudes(bDigs, aDigs), b.IsNegative),
            _ => new BetterBigInteger([0])
        };
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b) => a + (-b);

    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        var digits = a.GetDigits();
        if (digits.Length == 1 && digits[0] == 0)
            return new BetterBigInteger(Array.Empty<uint>());

        return new BetterBigInteger(digits.ToArray(), !a.IsNegative);
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b) =>
        throw new NotImplementedException();

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b) =>
        throw new NotImplementedException();


    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        var multiplier = new SimpleMultiplier();
        return multiplier.Multiply(a, b);
    }

    public static BetterBigInteger operator ~(BetterBigInteger a) => throw new NotImplementedException();

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b) =>
        throw new NotImplementedException();

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b) =>
        throw new NotImplementedException();

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b) =>
        throw new NotImplementedException();

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        if (shift < 0) return a >> -shift;
        if (shift == 0) return new BetterBigInteger(a.GetDigits().ToArray(), a.IsNegative);

        var digits = a.GetDigits();
        int limbShift = shift / 32;
        int bitShift = shift % 32;
        var result = new uint[digits.Length + limbShift + 1];

        for (int i = 0; i < digits.Length; i++)
        {
            result[i + limbShift] |= digits[i] << bitShift;

            // carry of shifted bits
            if (bitShift > 0)
                result[i + limbShift + 1] |= digits[i] >> (32 - bitShift);
        }

        return new BetterBigInteger(result, a.IsNegative);
    }

    public static BetterBigInteger operator >> (BetterBigInteger a, int shift)
    {
        if (shift < 0) return a << -shift;
        if (shift == 0) return new BetterBigInteger(a.GetDigits().ToArray(), a.IsNegative);

        var digits = a.GetDigits();
        int limbShift = shift / 32;
        int bitShift = shift % 32;
        var result = new uint[digits.Length];

        for (int i = digits.Length - 1; i - limbShift >= 0; i--)
        {
            result[i - limbShift] |= digits[i] >> bitShift;

            if (bitShift > 0 && i - limbShift - 1 >= 0)
                result[i - limbShift - 1] |= digits[i] << (32 - bitShift);
        }

        return new BetterBigInteger(result, a.IsNegative);
    }

    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;

    public override string ToString() => ToString(10);

    public string ToString(int radix)
    {
        if (radix < 2 || radix > 36)
            throw new ArgumentOutOfRangeException(nameof(radix), "Radix must be between 2 and 36");

        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var digits = GetDigits();

        if (digits.Length == 1 && digits[0] == 0)
            return "0";

        var sb = new StringBuilder();
        var curr = digits.ToArray();

        while (curr is not [0])
        {
            curr = DivideByUInt(curr, (uint)radix, out var rem);

            sb.Append(chars[(int)rem]);

            int normLen = curr.Length;
            while (normLen > 1 && curr[normLen - 1] == 0)
                normLen--;

            if (normLen != curr.Length)
                curr = curr[..normLen];
        }

        if (IsNegative)
            sb.Append('-');

        var chars2 = sb.ToString().ToCharArray();
        Array.Reverse(chars2);
        return new string(chars2);
    }

    private static uint[] DivideByUInt(ReadOnlySpan<uint> digitsSpan, uint divisor, out uint remainder)
    {
        var res = new uint[digitsSpan.Length];
        ulong rem = 0;

        for (int i = digitsSpan.Length - 1; i >= 0; i--)
        {
            // guaranteed no overflow
            // vot prufi
            // cur = rem * 2^32 + dig
            // cur < rem * 2^32 + 2^32
            // dig < 2^32, rem < div by def
            // cur < 2^32 * (rem+1) [rem + 1 <= div]
            // cur / div < 2^32 * ((rem+1) / div)-> <= 1
            // cur / div < 2^32
            ulong cur = (rem << 32) | digitsSpan[i];
            res[i] = (uint)(cur / divisor);
            rem = cur % divisor;
        }

        remainder = (uint)rem;
        return res;
    }
}