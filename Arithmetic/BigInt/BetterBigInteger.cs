using System.Numerics;
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
        var isNeg = false;
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
        uint carry = 0;

        for (var i = 0; i < result.Length - 1; i++)
        {
            var aVal = i < a.Length ? a[i] : 0u;
            var bVal = i < b.Length ? b[i] : 0u;

            var s0 = aVal + bVal;
            var c0 = s0 < aVal ? 1u : 0u;

            var s1 = s0 + carry;
            var c1 = s1 < s0 ? 1u : 0u;

            result[i] = s1;
            carry = c0 + c1;
        }

        result[^1] = carry;
        return result;
    }


    private static uint[] SubtractMagnitudes(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        var result = new uint[a.Length];
        uint borrow = 0;

        for (var i = 0; i < a.Length; i++)
        {
            var aVal = a[i];
            var bVal = i < b.Length ? b[i] : 0u;

            var s0 = aVal - bVal;
            var c0 = s0 > aVal ? 1u : 0u;

            var s1 = s0 - borrow;
            var c1 = s1 > s0 ? 1u : 0u;

            result[i] = s1;
            borrow = c0 + c1;
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
            return new BetterBigInteger([]);

        return new BetterBigInteger(digits.ToArray(), !a.IsNegative);
    }


    private int BitLength()
    {
        var digits = GetDigits();
        if (digits is [0]) return 0;

        return 32 * digits.Length - BitOperations.LeadingZeroCount(digits[^1]);
    }

    private static (BetterBigInteger quotient, BetterBigInteger remainder) DivRem(BetterBigInteger a,
        BetterBigInteger b)
    {
        if (b.GetDigits() is [0])
            throw new DivideByZeroException();


        switch (CompareMagnitude(a.GetDigits(), b.GetDigits()))
        {
            case -1:
                return (new BetterBigInteger([0]), a);

            case 0:
                return (new BetterBigInteger([1], a.IsNegative ^ b.IsNegative), new BetterBigInteger([0]));
        }

        var remainder = new BetterBigInteger(a.GetDigits().ToArray());
        var quotient = new BetterBigInteger([0]);
        var one = new BetterBigInteger([1]);

        var bAbs = new BetterBigInteger(b.GetDigits().ToArray()); // без знака
        var shift = a.BitLength() - b.BitLength();

        for (int i = shift; i >= 0; i--)
        {
            var shifted = bAbs << i;

            if (CompareMagnitude(remainder.GetDigits(), shifted.GetDigits()) < 0)
                continue;

            remainder = new BetterBigInteger(
                SubtractMagnitudes(remainder.GetDigits(), shifted.GetDigits())
            );
            quotient |= one << i;
        }

        var remIsNeg = a.IsNegative && quotient.GetDigits() is not [0];
        var quoIsNeg = a.IsNegative ^ b.IsNegative && quotient.GetDigits() is not [0];

        quotient = new BetterBigInteger(quotient.GetDigits().ToArray(), quoIsNeg);
        remainder = new BetterBigInteger(remainder.GetDigits().ToArray(), remIsNeg);

        return (quotient, remainder);
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b) => DivRem(a, b).quotient;

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b) => DivRem(a, b).remainder;


    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        var multiplier = new SimpleMultiplier();
        return multiplier.Multiply(a, b);
    }

    private static uint[] ToTwosComplement(BetterBigInteger a, int length)
    {
        var result = new uint[length];
        a.GetDigits().CopyTo(result.AsSpan());

        if (!a.IsNegative) return result;

        uint carry = 1;
        for (var i = 0; i < length; i++)
        {
            var flipped = ~result[i];
            var sum = flipped + carry;
            carry = sum < flipped ? 1u : 0u;
            result[i] = sum;
        }

        return result;
    }

    private static BetterBigInteger FromTwosComplement(uint[] data)
    {
        // mask 100000...0 & last limb
        var isNegative = (data[^1] & 0x80000000u) != 0;

        if (!isNegative)
            return new BetterBigInteger(data);

        var length = data.Length;
        var result = new uint[length];

        uint borrow = 1;
        for (var i = 0; i < length; i++)
        {
            var d = data[i];
            var diff = d - borrow;
            borrow = diff > d ? 1u : 0u;
            result[i] = ~diff;
        }

        return new BetterBigInteger(result, true);
    }

    private static BetterBigInteger BitwiseOp(
        BetterBigInteger a, BetterBigInteger b, Func<uint, uint, uint> op)
    {
        int len = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;
        var aTwoCompl = ToTwosComplement(a, len);
        var bTwoCompl = ToTwosComplement(b, len);

        var result = new uint[len];

        for (int i = 0; i < len; i++)
            result[i] = op(aTwoCompl[i], bTwoCompl[i]);

        return FromTwosComplement(result);
    }

    public static BetterBigInteger operator ~(BetterBigInteger a) => -(a + new BetterBigInteger([1]));

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b) =>
        BitwiseOp(a, b, (x, y) => x & y);

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b) =>
        BitwiseOp(a, b, (x, y) => x | y);

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b) =>
        BitwiseOp(a, b, (x, y) => x ^ y);

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

        if (a.IsNegative)
        {
            // арифметический сдвиг -((-a - 1) >> shift) - 1
            var abs = -a;
            var shifted = (abs - new BetterBigInteger([1])) >> shift;
            return -(shifted + new BetterBigInteger([1]));
        }

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
        uint rem = 0;

        for (int i = digitsSpan.Length - 1; i >= 0; i--)
        {
            var lo = digitsSpan[i] & 0xFFFF;
            var hi = digitsSpan[i] >> 16;

            // rem < divisor <= 36, rem * 2^16 не переполняет uint
            var hiVal = (rem << 16) | hi;
            var q1 = hiVal / divisor;
            var r1 = hiVal % divisor;

            var loVal = (r1 << 16) | lo;
            var q0 = loVal / divisor;
            rem = loVal % divisor;
            res[i] = (q1 << 16) | q0;
        }

        remainder = rem;
        return res;
    }
}