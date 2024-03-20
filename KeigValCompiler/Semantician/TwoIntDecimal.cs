using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

/* A lot of magic numbers in here :) */
internal struct TwoIntDecimal
{
    // Static fields.
    const int MAX_MANTISSA = 999_999_999;
    const int MANTISSA_LIMIT = 1_000_000_000;

    internal static TwoIntDecimal Pi { get; } = new(double.Pi);
    internal static TwoIntDecimal E { get; } = new(double.E);
    internal static TwoIntDecimal Tau { get; } = new(double.Tau);
    internal static TwoIntDecimal MaxValue { get; } = new(999_999_999, int.MaxValue - 1);
    internal static TwoIntDecimal MinValue { get; } = new(-999_999_999, int.MaxValue - 1);
    internal static TwoIntDecimal PositiveInfinity { get; } = new(999_999_999, int.MaxValue);
    internal static TwoIntDecimal NegativeInfinity { get; } = new(-999_999_999, int.MinValue);
    internal static TwoIntDecimal NaN { get; } = new(MANTISSA_LIMIT, 0);


    // Fields.
    public int Mantissa
    {
        get => _mantissa;
        set
        {
            _mantissa = value;
            if (_mantissa == 0)
            {
                Exponent = 0;
            }
        }
    }
    public int Exponent { get; set; }


    // Private fields.
    private int _mantissa;


    // Constructors.
    internal TwoIntDecimal(int mantissa, int exponent)
    {
        Mantissa = mantissa;
        Exponent = exponent;
    }

    internal TwoIntDecimal(long value)
    {
        int ValueExponent = CountDigitsInLong(value) - 1;
        const int REQUIRED_EXPONENT = 8;

        for (int i = ValueExponent; i > REQUIRED_EXPONENT; i--)
        {
            value /= 10;
        }
        for (int i = ValueExponent; i < REQUIRED_EXPONENT; i++)
        {
            value *= 10;
        }


        Mantissa = (int)value;
        Exponent = ValueExponent;
    }

    internal TwoIntDecimal(double value)
    {
        if (double.IsInfinity(value))
        {
            Mantissa = value > 0 ? PositiveInfinity.Mantissa : NegativeInfinity.Mantissa;
            Exponent = value > 0 ? PositiveInfinity.Exponent : NegativeInfinity.Exponent;
            return;
        }
        if (double.IsNaN(value))
        {
            Mantissa = NaN.Mantissa;
            Exponent = NaN.Exponent;
            return;
        }

        int ValueExponent = (int)Math.Floor(Math.Log10(Math.Abs(value)));
        const int REQUIRED_EXPONENT = 8;

        Mantissa = (int)(value / Math.Pow(10d, ValueExponent - REQUIRED_EXPONENT));
        Exponent = ValueExponent;
    }


    // Internal static methods.
    internal static bool TryParse(string number, out TwoIntDecimal dec)
    {
        TwoIntDecimal Result = new();

        if (number.Contains('e'))
        {
            return TryParseScientificNotation(number, out dec);
        }
        return TryParseRegular(number, out dec);
    }

    internal static bool IsNaN(TwoIntDecimal dec)
    {
        return Math.Abs(dec.Mantissa) > MAX_MANTISSA;
    }

    internal static bool IsInfinity(TwoIntDecimal dec)
    {
        return dec.Exponent == int.MaxValue;
    }

    internal static bool IsInteger(TwoIntDecimal dec) => GetFractionDigits(dec) == 0;

    internal static int Sign(TwoIntDecimal dec)
    {
        if (dec.Mantissa < 0)
        {
            return -1;
        }
        if (dec.Mantissa > 0)
        {
            return 1;
        }
        return 0;
    }

    internal static TwoIntDecimal Pow(TwoIntDecimal dec, TwoIntDecimal power)
    {
        throw new NotImplementedException();
    }

    internal static TwoIntDecimal Sqrt(TwoIntDecimal dec)
    {
        int Iterations = Math.Min(10 + dec.Exponent / 10, 1000);

        TwoIntDecimal OneSecond = 0.5d;
        TwoIntDecimal X = 1d;

        for (int i = 0; i < Iterations; i++)
        {
            X = OneSecond * (X + (dec / X));
        }

        return X;
    }

    internal static TwoIntDecimal Cbrt(TwoIntDecimal dec)
    {
        int Iterations = Math.Min(10 + dec.Exponent / 10, 1000);

        TwoIntDecimal OneThird = 1d / 3d;
        TwoIntDecimal X = 1d;

        for (int i = 0; i < Iterations; i++)
        {
            X = OneThird * ((dec / (X * X)) + (2 * X));
        }

        return X;
    }

    internal static TwoIntDecimal Ceil(TwoIntDecimal dec) => MoveTowardsSign(dec, 1);

    internal static TwoIntDecimal Floor(TwoIntDecimal dec) => MoveTowardsSign(dec, -1);

    internal static TwoIntDecimal Round(TwoIntDecimal dec)
    {
        // Not optimized but works alright.
        int LeadingDigit = GetFractionDigits(dec);
        LeadingDigit = LeadingDigit / (int)Math.Pow(10d, GetMantissaDigitCountInFraction(dec) - 1);

        int TargetSign = (LeadingDigit >= 5 ? 1 : -1) * Sign(dec);
        return MoveTowardsSign(dec, TargetSign);
    }

    internal static TwoIntDecimal Trunctate(TwoIntDecimal dec)
    {
        throw new NotImplementedException();
    }


    // Private static methods.
    private static int CountDigitsInLong(long value)
    {
        value = Math.Abs(value);
        int Digits = 0;

        while (value > 0)
        {
            value /= 10;
            Digits++;
        }

        return Digits;
    }

    private static (TwoIntDecimal Bigger, TwoIntDecimal Smaller) GetAdjustedTwoIntDecimals(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (a.Exponent < b.Exponent)
        {
            (a, b) = (b, a);
        }

        const int MAX_SHIFTS = 9;
        for (int i = 0; i < Math.Min(MAX_SHIFTS, a.Exponent - b.Exponent); i++)
        {
            b.Mantissa /= 10;
        }

        return (a, b);
    }

    private static bool TryParseScientificNotation(string number, out TwoIntDecimal result)
    {
        result = default;

        if (number.EndsWith('e'))
        {
            return false;
        }
       
        string MantissaStr = number.Substring(0, number.IndexOf('e'));
        string ExponentStr = number.Substring(number.IndexOf('e') + 1);

        if ((MantissaStr.Length == 0) || ExponentStr.Length == 0)
        {
            return false;
        }

        if (!double.TryParse(MantissaStr, CultureInfo.InvariantCulture, out double Mantissa))
        {
            return false;
        }
        if (!int.TryParse(ExponentStr, out int Exponent))
        {
            return false;
        }

        result = new(Mantissa);
        result.Exponent = Exponent;
        return true;
    }

    private static bool TryParseRegular(string number, out TwoIntDecimal result)
    {
        result = default;

        int MANTISSA_DIGIT_COUNT = 9;
        int Index = 0;


        foreach (Char Character in number)
        {
            if (char.IsDigit(Character)) { }
        }

        throw new NotImplementedException();
    }

    private static int GetFractionDigits(TwoIntDecimal dec)
    {
        return Math.Abs(dec.Mantissa) % (int)Math.Pow(10d, GetMantissaDigitCountInFraction(dec));
    }

    private static TwoIntDecimal MoveTowardsSign(TwoIntDecimal dec, int sign)
    {
        int FractionDigits = GetFractionDigits(dec);
        if (FractionDigits == 0)
        {
            return dec;
        }

        int MantissaSign = Sign(dec);
        dec.Mantissa -= FractionDigits * MantissaSign;

        if (MantissaSign == sign)
        {
            dec += sign;
        }

        return dec;
    }

    private static int GetMantissaDigitCountInFraction(TwoIntDecimal dec) => Math.Clamp(8 - dec.Exponent, 0, 9);


    // Inherited methods.
    public override string ToString()
    {
        if (IsNaN(this))
        {
            return "NaN";
        }
        else if (IsInfinity(this))
        {
            return Mantissa >= 0 ? "Positive Infinity" : "Negative Infinity";
        }

        const int SCIENTIFIC_NOTATION_MIN_EXPONENT = 18;
        if (Math.Abs(Exponent) >= SCIENTIFIC_NOTATION_MIN_EXPONENT)
        {
            return $"{(Mantissa > 0 ? null : '-')}{Math.Abs(Mantissa) / 100_000_000}.{Math.Abs(Mantissa) % 100_000_000}e{Exponent}";
        }

        const int MANTISSA_DIGIT_COUNT = 9;
        StringBuilder Builder = new();

        if (Exponent < 0)
        {
            Builder.Append("0.");
            for (int i = Exponent + 1; i < 0; i++)
            {
                Builder.Append('0');
            }
            Builder.Append(Math.Abs(Mantissa));
        }
        else
        {
            Builder.Append(Mantissa != 0 ? Math.Abs(Mantissa) : 0);
            if ((Exponent < MANTISSA_DIGIT_COUNT - 1) && Mantissa != 0)
            {
                Builder.Insert(Exponent + 1, '.');
            }
            for (int i = MANTISSA_DIGIT_COUNT; i <= Exponent; i++)
            {
                Builder.Append("0");
            }
        }

        if (Mantissa < 0)
        {
            Builder.Insert(0, '-');
        }

        return Builder.ToString();
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is TwoIntDecimal)
        {
            return this == (TwoIntDecimal)obj;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Mantissa.GetHashCode() + Exponent.GetHashCode();
    }


    // Operators.
    public static bool operator ==(TwoIntDecimal a, TwoIntDecimal b)
    {
        return (a.Mantissa == b.Mantissa) && (a.Exponent == b.Exponent) && !IsNaN(a) && !IsNaN(b);
    }

    public static bool operator !=(TwoIntDecimal a, TwoIntDecimal b)
    {
        return ((a.Mantissa != b.Mantissa) || (a.Exponent != b.Exponent)) && !IsNaN(a) && !IsNaN(b);
    }

    public static bool operator >(TwoIntDecimal a, TwoIntDecimal b)
    {
        return (a.Exponent != b.Exponent ? (a.Exponent > b.Exponent) : (a.Mantissa > b.Mantissa)) && !IsNaN(a) && !IsNaN(b);
    }

    public static bool operator <(TwoIntDecimal a, TwoIntDecimal b)
    {
        return (a.Exponent != b.Exponent ? (a.Exponent < b.Exponent) : (a.Mantissa < b.Mantissa)) && !IsNaN(a) && !IsNaN(b);
    }

    public static bool operator >=(TwoIntDecimal a, TwoIntDecimal b)
    {
        return (a.Exponent != b.Exponent ? (a.Exponent >= b.Exponent) : (a.Mantissa >= b.Mantissa)) && !IsNaN(a) && !IsNaN(b);
    }

    public static bool operator <=(TwoIntDecimal a, TwoIntDecimal b)
    {
        return (a.Exponent != b.Exponent ? (a.Exponent <= b.Exponent) : (a.Mantissa <= b.Mantissa)) && !IsNaN(a) && !IsNaN(b);
    }

    public static TwoIntDecimal operator +(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (IsNaN(a) || IsNaN(b))
        {
            return NaN;
        }

        (a, b) = GetAdjustedTwoIntDecimals(a, b);

        a.Mantissa += b.Mantissa;
        if (Math.Abs(a.Mantissa) > MAX_MANTISSA)
        {
            a.Exponent++;
            a.Mantissa /= 10;
        }

        return a;
    }

    public static TwoIntDecimal operator -(TwoIntDecimal a)
    {
        a.Mantissa = -a.Mantissa;
        return a;
    }

    public static TwoIntDecimal operator -(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (IsNaN(a) || IsNaN(b))
        {
            return NaN;
        }

        return a + (-b);
    }

    public static TwoIntDecimal operator *(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (IsNaN(a) || IsNaN(b))
        {
            return NaN;
        }

        a.Exponent += b.Exponent;
        long Mantissa = (long)a.Mantissa * (long)b.Mantissa;
        int ValueExponent = CountDigitsInLong(Mantissa) - 1;
        const int TARGET_EXPONENT = 8;
        const int REQUIRED_CARRY_OVER_EXPONENT = 17;

        if (ValueExponent >= REQUIRED_CARRY_OVER_EXPONENT)
        {
            a.Exponent += 1;
        }

        for (int i = ValueExponent; i > TARGET_EXPONENT; i--)
        {
            Mantissa /= 10;
        }

        a.Mantissa = (int)Mantissa;
        return a;
    }

    public static TwoIntDecimal operator /(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (IsNaN(a) || IsNaN(b))
        {
            return NaN;
        }

        a.Exponent -= b.Exponent;
        double Mantissa = (double)a.Mantissa / (double)b.Mantissa * 100_000_000d;
        while (Mantissa < 100_000_000)
        {
            Mantissa *= 10d;
            a.Exponent -= 1;
        }
        while (Mantissa >= 1_000_000_000)
        {
            Mantissa *= 0.1d;
            a.Exponent += 1;
        }

        a.Mantissa = (int)Mantissa;
        return a;
    }

    public static TwoIntDecimal operator ++(TwoIntDecimal dec)
    {
        return dec += 1;
    }

    public static TwoIntDecimal operator --(TwoIntDecimal dec)
    {
        return dec -= 1;
    }

    public static implicit operator TwoIntDecimal(byte number)
    {
        return new TwoIntDecimal((double)number);
    }

    public static implicit operator TwoIntDecimal(short number)
    {
        return new TwoIntDecimal((double)number);
    }

    public static implicit operator TwoIntDecimal(int number)
    {
        return new TwoIntDecimal((double)number);
    }

    public static implicit operator TwoIntDecimal(long number)
    {
        return new TwoIntDecimal((double)number);
    }

    public static implicit operator TwoIntDecimal(float number)
    {
        return new TwoIntDecimal((double)number);
    }

    public static implicit operator TwoIntDecimal(double number)
    {
        return new TwoIntDecimal(number);
    }

    public static explicit operator double(TwoIntDecimal dec)
    {
        return (double)dec.Mantissa / 100_000_000d * Math.Pow(10, dec.Exponent);
    }

    public static explicit operator float(TwoIntDecimal dec)
    {
        return (float)dec.Mantissa / 100_000_000f * MathF.Pow(10, dec.Exponent);
    }

    public static explicit operator long(TwoIntDecimal dec)
    {
        return (long)((double)dec.Mantissa / 100_000_000d * Math.Pow(10, dec.Exponent));
    }

    public static explicit operator int(TwoIntDecimal dec)
    {
        return (int)((double)dec.Mantissa / 100_000_000d * Math.Pow(10, dec.Exponent));
    }

    public static explicit operator short(TwoIntDecimal dec)
    {
        return (short)((double)dec.Mantissa / 100_000_000d * Math.Pow(10, dec.Exponent));
    }

    public static explicit operator byte(TwoIntDecimal dec)
    {
        return (byte)((double)dec.Mantissa / 100_000_000d * Math.Pow(10, dec.Exponent));
    }
}