using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace KeigValCompiler.Semantician;

/* A lot of magic numbers in here :) */
internal readonly struct TwoIntDecimal
{
    // Internal static fields.
    internal const int MAX_MANTISSA = 999_999_999;
    internal const int MANTISSA_LIMIT = 1_000_000_000;
    internal const int MANTISSA_DEFAULT_VALUE = 100_000_000;
    internal const int MANTISSA_DIGIT_COUNT = 9;
    internal const int MANTISSA_EXPONENT = MANTISSA_DIGIT_COUNT - 1;

    internal static TwoIntDecimal Pi { get; } = new(double.Pi);
    internal static TwoIntDecimal E { get; } = new(double.E);
    internal static TwoIntDecimal Tau { get; } = new(double.Tau);
    internal static TwoIntDecimal MaxValue { get; } = new(MAX_MANTISSA, int.MaxValue - 1);
    internal static TwoIntDecimal MinValue { get; } = new(-MAX_MANTISSA, int.MaxValue - 1);
    internal static TwoIntDecimal Epsilon { get; } = new(1, int.MinValue);
    internal static TwoIntDecimal PositiveInfinity { get; } = new(MAX_MANTISSA, int.MaxValue);
    internal static TwoIntDecimal NegativeInfinity { get; } = new(-MAX_MANTISSA, int.MaxValue);
    internal static TwoIntDecimal NaN { get; } = new(MANTISSA_LIMIT, 0);


    // Fields.
    public int Mantissa { get; private init; }
    public int Exponent
    {
        get => _exponent;
        private init
        {
            _exponent = value;
            if (Mantissa == 0)
            {
                Exponent = 0;
            }
        }
    }


    // Private fields.
    private readonly int _exponent;


    // Constructors.
    internal TwoIntDecimal(int mantissa, int exponent)
    {
        Mantissa = mantissa;
        Exponent = exponent;
    }

    internal TwoIntDecimal(long value)
    {
        int DigitsInLong = CountDigitsInLong(value);
        int RequiredDigitsInLong = MANTISSA_DIGIT_COUNT;
        long NormalizedValue = value;

        for (int i = DigitsInLong; i > RequiredDigitsInLong; i--)
        {
            NormalizedValue /= 10L;
        }
        for (int i = DigitsInLong; i < RequiredDigitsInLong; i++)
        {
            NormalizedValue *= 10L;
        }


        Mantissa = (int)NormalizedValue;
        Exponent = DigitsInLong - 1;
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

        Mantissa = GetMantissaFromDouble(value);
        Exponent = (int)Math.Floor(Math.Log10(Math.Abs(value)));
    }


    // Internal static methods.
    internal static bool TryParse(string number, out TwoIntDecimal dec)
    {
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

    internal static TwoIntDecimal Log10(TwoIntDecimal dec)
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

    internal static TwoIntDecimal NthRoot(TwoIntDecimal dec)
    {
        throw new NotImplementedException();
    }

    internal static TwoIntDecimal Ceil(TwoIntDecimal dec) => MoveTowardsSign(dec, 1);

    internal static TwoIntDecimal Floor(TwoIntDecimal dec) => MoveTowardsSign(dec, -1);

    internal static TwoIntDecimal Round(TwoIntDecimal dec)
    {
        int LeadingDigit = GetFractionDigits(dec);
        LeadingDigit = LeadingDigit / (int)Math.Pow(10d, GetMantissaDigitCountInFraction(dec) - 1);

        int TargetSign = (LeadingDigit >= 5 ? 1 : -1) * Sign(dec);
        return MoveTowardsSign(dec, TargetSign);
    }

    internal static TwoIntDecimal Truncate(TwoIntDecimal dec)
    {
        return new(dec.Mantissa - GetFractionDigits(dec) * Sign(dec), dec.Exponent);
    }


    // Private static methods.
    private static int CountDigitsInLong(long value)
    {
        int Digits = 0;

        while (value != 0)
        {
            value /= 10;
            Digits++;
        }

        return Digits;
    }

    private static int GetMantissaFromDouble(double value)
    {
        if ((value == 0d) || (value == -0d))
        {
            return 0;
        }

        int CurrentExponent = (int)Math.Log10(Math.Abs(value));
        return (int)(value * Math.Pow(10d, MANTISSA_EXPONENT - CurrentExponent));
    }

    private static (TwoIntDecimal Bigger, TwoIntDecimal Smaller) GetAdjustedTwoIntDecimals(TwoIntDecimal a, TwoIntDecimal b)
    {
        TwoIntDecimal A = a;
        TwoIntDecimal B = b;

        if (a.Exponent < b.Exponent)
        {
            (A, B) = (B, A);
        }

        int NewBMantissa = B.Mantissa;
        for (int i = 0; i < Math.Min(MANTISSA_DIGIT_COUNT, a.Exponent - b.Exponent); i++)
        {
            NewBMantissa /= 10;
        }

        return (a, new TwoIntDecimal(NewBMantissa, B.Exponent));
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

        if (double.IsNaN(Mantissa))
        {
            result = NaN;
        }
        else if (double.IsInfinity(Mantissa))
        {
            result = Mantissa > 0 ? PositiveInfinity : NegativeInfinity;
        }
        else
        {
            result = new(GetMantissaFromDouble(Mantissa), Exponent);
        }

        return true;
    }

    private static bool TryParseRegular(string number, out TwoIntDecimal result)
    {
        result = default;

        foreach (char Character in number)
        {
            if (char.IsDigit(Character))
            {

            }
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

        int NewMantissa = dec.Mantissa - FractionDigits + Sign(dec);
        int NewExponent = dec.Exponent + (Math.Sign(NewMantissa) * (NewMantissa / MANTISSA_LIMIT));

        return new(NewMantissa, NewExponent);
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

        int NewMantissa = a.Mantissa + b.Mantissa;
        int NewExponent = a.Exponent;
        if (Math.Abs(a.Mantissa) > MAX_MANTISSA)
        {
            NewExponent++;
            NewMantissa /= 10;
        }

        return new(NewMantissa, NewExponent);
    }

    public static TwoIntDecimal operator -(TwoIntDecimal a)
    {
        return new(-a.Mantissa, a.Exponent);
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

        double Mantissa = a.Mantissa * ((double)b.Mantissa / MANTISSA_DEFAULT_VALUE);
        int NewExponent = a.Exponent + b.Exponent;
        int NormalizedMantissa;
        if (Math.Abs(Mantissa) > MAX_MANTISSA)
        {
            NormalizedMantissa = (int)(Mantissa * 0.1d);
            NewExponent++;
        }
        else
        {
            NormalizedMantissa = (int)Mantissa;
        }
       
        return new(NormalizedMantissa, NewExponent);
    }

    public static TwoIntDecimal operator /(TwoIntDecimal a, TwoIntDecimal b)
    {
        // Implemented as in DataPacks (which is why it is so complex).
        if (IsNaN(a) || IsNaN(b) || (b.Mantissa == 0))
        {
            return NaN;
        }

        double MantissaDivisionResult = (double)a.Mantissa / b.Mantissa;
        int NewMantissa;
        int NewExponent = a.Exponent - b.Exponent;
        if (MantissaDivisionResult < 1d)
        {
            NewExponent--;
            NewMantissa = (int)(MantissaDivisionResult * 10d * MANTISSA_DEFAULT_VALUE);
        }
        else
        {
            NewMantissa = (int)(MantissaDivisionResult * MANTISSA_DEFAULT_VALUE);
        }

        return new(NewMantissa, NewExponent);
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
        if (IsNaN(dec))
        {
            return double.NaN;
        }
        if (IsInfinity(dec))
        {
            return Sign(dec) == 1 ? double.PositiveInfinity : double.NegativeInfinity;
        }

        return (double)dec.Mantissa / MANTISSA_DEFAULT_VALUE * Math.Pow(10f, dec.Exponent);
    }

    public static explicit operator float(TwoIntDecimal dec)
    {
        if (IsNaN(dec))
        {
            return float.NaN;
        }
        if (IsInfinity(dec))
        {
            return Sign(dec) == 1 ? float.PositiveInfinity : float.NegativeInfinity;
        }

        return (float)dec.Mantissa / MANTISSA_DEFAULT_VALUE * MathF.Pow(10f, dec.Exponent);
    }

    public static explicit operator long(TwoIntDecimal dec)
    {
        return (long)((double)dec.Mantissa / MANTISSA_DEFAULT_VALUE * Math.Pow(10d, dec.Exponent));
    }

    public static explicit operator int(TwoIntDecimal dec)
    {
        return (int)((double)dec.Mantissa / MANTISSA_DEFAULT_VALUE * Math.Pow(10d, dec.Exponent));
    }

    public static explicit operator short(TwoIntDecimal dec)
    {
        return (short)((double)dec.Mantissa / MANTISSA_DEFAULT_VALUE * Math.Pow(10d, dec.Exponent));
    }

    public static explicit operator byte(TwoIntDecimal dec)
    {
        return (byte)((double)dec.Mantissa / MANTISSA_DEFAULT_VALUE * Math.Pow(10d, dec.Exponent));
    }
}