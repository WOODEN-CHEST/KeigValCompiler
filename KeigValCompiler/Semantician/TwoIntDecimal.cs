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
    internal const char DECIMAL_SEPARATOR_PERIOD = '.';
    internal const char DECIMAL_SEPARATOR_COMMA = ',';
    internal const char NEGATION_SYMBOL = '-';

    internal static TwoIntDecimal Pi { get; } = new(double.Pi);
    internal static TwoIntDecimal E { get; } = new(double.E);
    internal static TwoIntDecimal Tau { get; } = new(double.Tau);
    internal static TwoIntDecimal MaxValue { get; } = new(MAX_MANTISSA, int.MaxValue - 1);
    internal static TwoIntDecimal MinValue { get; } = new(-MAX_MANTISSA, int.MaxValue - 1);
    internal static TwoIntDecimal Epsilon { get; } = new(MANTISSA_DEFAULT_VALUE, int.MinValue);
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
            _exponent = Mantissa != 0 ? value : 0;
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
        if (string.IsNullOrWhiteSpace(number))
        {
            dec = default;
            return false;
        }

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

    internal static TwoIntDecimal Log(TwoIntDecimal value, TwoIntDecimal numberBase)
    {
        throw new NotImplementedException();
    }

    internal static TwoIntDecimal DegToRad(TwoIntDecimal deg)
    {
        throw new NotImplementedException();
    }

    internal static TwoIntDecimal RadToDeg(TwoIntDecimal rad)
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
        int LeadingDigits = GetFractionDigits(dec);

        int TargetSign = (LeadingDigits >= 5 ? 1 : -1) * Sign(dec);
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

    private static int GetDigitAtIndex(long value, int indexFromLSD)
    {
        long Divisor = 1;
        for (int i = 0; i < indexFromLSD; i++)
        {
            Divisor *= 10L;
        }

        return (int)(Math.Abs(value) / Divisor % 10L) * Math.Sign(value);
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

        int Mantissa = 0;
        int MantissaDigitWeight = MANTISSA_DEFAULT_VALUE;
        int Exponent = 0;
        bool FoundSeparator = false;
        bool IsNegative = false;

        foreach (char Character in number)
        {
            if (!IsNegative && (Character == NEGATION_SYMBOL))
            {
                IsNegative = true;
            }
            else if (char.IsAsciiDigit(Character))
            {
                int Digit = Character - '0';

                if ((Mantissa == 0) && FoundSeparator)
                {
                    Exponent--;
                }
                else if ((Mantissa != 0) && !FoundSeparator)
                {
                    Exponent++;
                }

                if (MantissaDigitWeight != 0)
                {
                    Mantissa += MantissaDigitWeight * Digit;
                    MantissaDigitWeight /= 10;
                }
            }
            else if (!FoundSeparator && ((Character == DECIMAL_SEPARATOR_PERIOD) || (Character == DECIMAL_SEPARATOR_COMMA)))
            {
                FoundSeparator = true;
            }
            else
            {
                return false;
            }
        }

        result = new(Mantissa * (IsNegative ? -1 : 1), Exponent);

        return true;
    }

    private static int GetFractionDigits(TwoIntDecimal dec)
    {
        int Digits = dec.Mantissa;
        int DigitMask = 1;
        int DigitCountInFraction = GetMantissaDigitCountInFraction(dec);
        for (int i = 0; i < DigitCountInFraction; i++)
        {
            DigitMask *= 10;
        }
        return dec.Mantissa % DigitMask;
    }

    private static TwoIntDecimal MoveTowardsSign(TwoIntDecimal dec, int sign)
    {
        int FractionDigits = GetFractionDigits(dec);

        int NewMantissa = dec.Mantissa - FractionDigits + Sign(dec);
        int NewExponent = dec.Exponent + (Math.Sign(NewMantissa) * (NewMantissa / MANTISSA_LIMIT));

        return new(NewMantissa, NewExponent);
    }

    private static int GetMantissaDigitCountInFraction(TwoIntDecimal dec) =>
        Math.Clamp(MANTISSA_EXPONENT - dec.Exponent, 0, MANTISSA_DIGIT_COUNT);

    private static string GetScientificNotationString(TwoIntDecimal dec)
    {
        return $"{(dec.Mantissa > 0 ? null : '-')}{Math.Abs(dec.Mantissa) / MANTISSA_DEFAULT_VALUE}" +
                $".{Math.Abs(dec.Mantissa).ToString().Substring(1)}e{dec.Exponent}";
    }

    private static string GetRegularString(TwoIntDecimal dec)
    {
        StringBuilder Builder = new();

        if (dec.Mantissa < 0)
        {
            Builder.Append('-');
        }

        if (dec.Exponent < 0)
        {
            Builder.Append("0.");
            for (int i = dec.Exponent + 1; i < 0; i++)
            {
                Builder.Append('0');
            }
            Builder.Append(Math.Abs(dec.Mantissa));
        }
        else
        {
            Builder.Append(Math.Abs(dec.Mantissa));
            if ((dec.Exponent < MANTISSA_EXPONENT) && dec.Mantissa != 0)
            {
                Builder.Insert(dec.Exponent + (dec.Mantissa < 0 ? 2 : 1), '.');
            }
            for (int i = MANTISSA_EXPONENT; i < dec.Exponent; i++)
            {
                Builder.Append('0');
            }
        }

        return Builder.ToString();
    }


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
            return GetScientificNotationString(this);
        }
        return GetRegularString(this);
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
        // Implemented as is in DataPacks (which is why it is so complex).
        if (IsNaN(a) || IsNaN(b))
        {
            return NaN;
        }
        if (b.Mantissa == 0)
        {
            return Sign(a) == 1 ? PositiveInfinity : NegativeInfinity;
        }

        int ResultingMantissa = 0;
        long PickedNumber = a.Mantissa;
        int NewExponent = a.Exponent - b.Exponent - (a.Mantissa < b.Mantissa ? 1 : 0);

        while (Math.Abs(ResultingMantissa) < MANTISSA_DEFAULT_VALUE)
        {
            while ((PickedNumber < b.Mantissa) && (Math.Abs(ResultingMantissa) < MANTISSA_DEFAULT_VALUE / 10))
            {
                PickedNumber *= 10L;
                ResultingMantissa *= 10;
            }

            ResultingMantissa *= 10;
            ResultingMantissa += (int)(PickedNumber / b.Mantissa);
            PickedNumber %= b.Mantissa;
            PickedNumber *= 10L;
        }

        return new(ResultingMantissa, NewExponent);
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