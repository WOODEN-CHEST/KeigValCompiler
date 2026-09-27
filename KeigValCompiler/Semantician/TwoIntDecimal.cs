using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace KeigValCompiler.Semantician;

/* A base-10 software floating point number built from two 32 bit signed integers.
 *
 *     value = Mantissa * 10^(Exponent - MANTISSA_EXPONENT)
 *
 * The mantissa holds nine significant decimal digits and carries the sign, so a finite non-zero
 * value always has a magnitude in [MIN_MANTISSA, MAX_MANTISSA]. Zero is the single representation
 * (0, 0); there is no negative zero.
 *
 * This type is a specification of the runtime's arithmetic rather than a convenience for the
 * compiler. The datapack backend has to emit the same algorithms as scoreboard commands, so every
 * method that a compiled program could reach at runtime obeys two rules:
 *
 *   1. Only int addition, subtraction, multiplication, division and remainder are used. No long,
 *      no double, no Math.* helper that a scoreboard cannot express.
 *   2. No intermediate value ever leaves the 32 bit signed range, and division and remainder are
 *      only ever applied to non-negative operands. C# truncates towards zero while a Minecraft
 *      scoreboard '/=' floors, and on non-negative operands the two agree.
 *
 * TryParse, Parse, ToString and the double, float and long conversions are the exception: they turn
 * source literals into constants and constants back into text, which happens in the compiler and
 * never in a datapack. They are marked as such where they appear.
 *
 * Special values follow IEEE-754 in spirit: arithmetic never throws, overflow saturates to an
 * infinity, underflow flushes to zero, and every undefined operation produces NaN. Rounding is
 * round-half-away-from-zero rather than IEEE's round-half-to-even, because it costs one comparison
 * in commands instead of three. */
internal readonly struct TwoIntDecimal
{
    // Internal static fields.
    /* Mantissa shape. */
    internal const int MAX_MANTISSA = 999_999_999;
    internal const int MIN_MANTISSA = 100_000_000;
    internal const int MANTISSA_LIMIT = 1_000_000_000;
    internal const int MANTISSA_DIGIT_COUNT = 9;
    internal const int MANTISSA_EXPONENT = MANTISSA_DIGIT_COUNT - 1;

    /* Exponent range. The usable range is deliberately narrower than int so that every exponent
     * sum, difference and adjustment this type performs stays inside 32 bits without a check:
     * the widest of them is MAX_EXPONENT - MIN_EXPONENT, which is still below int.MaxValue. */
    internal const int MAX_EXPONENT = 999_999_999;
    internal const int MIN_EXPONENT = -MAX_EXPONENT;
    internal const int INFINITY_EXPONENT = int.MaxValue;

    /* Text. */
    internal const char DECIMAL_SEPARATOR_PERIOD = KGVL.DECIMAL_SEPARATOR;
    internal const char DECIMAL_SEPARATOR_COMMA = ',';
    internal const char NEGATION_SYMBOL = '-';
    internal const char POSITIVE_SYMBOL = '+';
    internal const char EXPONENT_SYMBOL = KGVL.DECIMAL_EXPONENT;
    internal const string NAN_TEXT = "NaN";
    internal const string POSITIVE_INFINITY_TEXT = "Infinity";
    internal const string NEGATIVE_INFINITY_TEXT = "-Infinity";

    /* Special values. */
    internal static TwoIntDecimal NaN { get; } = new(MANTISSA_LIMIT, 0);
    internal static TwoIntDecimal PositiveInfinity { get; } = new(MAX_MANTISSA, INFINITY_EXPONENT);
    internal static TwoIntDecimal NegativeInfinity { get; } = new(-MAX_MANTISSA, INFINITY_EXPONENT);
    internal static TwoIntDecimal MaxValue { get; } = new(MAX_MANTISSA, MAX_EXPONENT);
    internal static TwoIntDecimal MinValue { get; } = new(-MAX_MANTISSA, MAX_EXPONENT);
    internal static TwoIntDecimal Epsilon { get; } = new(MIN_MANTISSA, MIN_EXPONENT);

    /* Common values. */
    internal static TwoIntDecimal Zero { get; } = new(0, 0);
    internal static TwoIntDecimal One { get; } = new(MIN_MANTISSA, 0);
    internal static TwoIntDecimal Two { get; } = new(200_000_000, 0);
    internal static TwoIntDecimal Ten { get; } = new(MIN_MANTISSA, 1);
    internal static TwoIntDecimal Half { get; } = new(500_000_000, -1);

    /* Mathematical constants. */
    internal static TwoIntDecimal Pi { get; } = new(314_159_265, 0);
    internal static TwoIntDecimal Tau { get; } = new(628_318_531, 0);
    internal static TwoIntDecimal E { get; } = new(271_828_183, 0);


    // Private static fields.
    private static readonly int[] POWERS_OF_TEN = new int[]
    {
        1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000
    };


    // Fields.
    internal int Mantissa { get; }
    internal int Exponent { get; }


    // Constructors.
    /* The raw constructor trusts its arguments and normalises nothing, so it is private. Everything
     * outside this type builds values through FromComponents, which normalises. */
    private TwoIntDecimal(int mantissa, int exponent)
    {
        Mantissa = mantissa;
        Exponent = exponent;
    }

    internal TwoIntDecimal(int value)
    {
        TwoIntDecimal Converted = FromInt32(value);
        Mantissa = Converted.Mantissa;
        Exponent = Converted.Exponent;
    }


    // Internal static methods.
    /* Construction. */
    /* Builds a value from a mantissa of any magnitude and an exponent, normalising the mantissa
     * back into [MIN_MANTISSA, MAX_MANTISSA] and saturating an exponent that leaves the usable
     * range to an infinity or to zero. This is the only way to create a value from raw parts. */
    internal static TwoIntDecimal FromComponents(int mantissa, int exponent)
    {
        if (mantissa == 0)
        {
            return Zero;
        }

        int Sign = mantissa < 0 ? -1 : 1;
        int Magnitude = GetMagnitude(mantissa);
        int ResultExponent = exponent;

        int DroppedDigit = 0;
        while (Magnitude > MAX_MANTISSA)
        {
            DroppedDigit = Magnitude % 10;
            Magnitude /= 10;
            ResultExponent++;
        }
        if (DroppedDigit >= 5)
        {
            Magnitude++;
            if (Magnitude > MAX_MANTISSA)
            {
                Magnitude /= 10;
                ResultExponent++;
            }
        }

        while (Magnitude < MIN_MANTISSA)
        {
            Magnitude *= 10;
            ResultExponent--;
        }

        if (ResultExponent > MAX_EXPONENT)
        {
            return Sign < 0 ? NegativeInfinity : PositiveInfinity;
        }
        if (ResultExponent < MIN_EXPONENT)
        {
            return Zero;
        }
        return new(Sign * Magnitude, ResultExponent);
    }

    /* Multiplies by a power of ten. The mantissa is untouched, so this is exact whenever the
     * result is representable at all. */
    internal static TwoIntDecimal ScaleByPowerOfTen(TwoIntDecimal dec, int power)
    {
        if (IsNaN(dec) || IsInfinity(dec) || (dec.Mantissa == 0))
        {
            return dec;
        }

        /* Both operands are inside the usable exponent range, so the sum cannot wrap. */
        int ResultExponent = dec.Exponent + Clamp(power, MIN_EXPONENT, MAX_EXPONENT);
        if (ResultExponent > MAX_EXPONENT)
        {
            return dec.Mantissa < 0 ? NegativeInfinity : PositiveInfinity;
        }
        if (ResultExponent < MIN_EXPONENT)
        {
            return Zero;
        }
        return new(dec.Mantissa, ResultExponent);
    }

    /* Classification. */
    internal static bool IsNaN(TwoIntDecimal dec)
    {
        return (dec.Mantissa > MAX_MANTISSA) || (dec.Mantissa < -MAX_MANTISSA);
    }

    internal static bool IsInfinity(TwoIntDecimal dec)
    {
        return (dec.Exponent == INFINITY_EXPONENT) && !IsNaN(dec);
    }

    internal static bool IsPositiveInfinity(TwoIntDecimal dec) => IsInfinity(dec) && (dec.Mantissa > 0);

    internal static bool IsNegativeInfinity(TwoIntDecimal dec) => IsInfinity(dec) && (dec.Mantissa < 0);

    internal static bool IsFinite(TwoIntDecimal dec) => !IsNaN(dec) && !IsInfinity(dec);

    internal static bool IsZero(TwoIntDecimal dec) => dec.Mantissa == 0;

    internal static bool IsInteger(TwoIntDecimal dec)
    {
        if (!IsFinite(dec))
        {
            return false;
        }
        return GetFractionDigits(dec) == 0;
    }

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

    /* Selection. */
    internal static TwoIntDecimal Abs(TwoIntDecimal dec)
    {
        if (IsNaN(dec) || (dec.Mantissa >= 0))
        {
            return dec;
        }
        return new(-dec.Mantissa, dec.Exponent);
    }

    internal static TwoIntDecimal Min(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (IsNaN(a) || IsNaN(b))
        {
            return NaN;
        }
        return Compare(a, b) <= 0 ? a : b;
    }

    internal static TwoIntDecimal Max(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (IsNaN(a) || IsNaN(b))
        {
            return NaN;
        }
        return Compare(a, b) >= 0 ? a : b;
    }

    internal static TwoIntDecimal Clamp(TwoIntDecimal dec, TwoIntDecimal min, TwoIntDecimal max)
    {
        return Min(Max(dec, min), max);
    }

    /* Rounding. */
    internal static TwoIntDecimal Truncate(TwoIntDecimal dec) =>
        RoundAtDecimalPoint(dec, RoundingMode.TowardsZero);

    internal static TwoIntDecimal Floor(TwoIntDecimal dec) => RoundAtDecimalPoint(dec, RoundingMode.Down);

    internal static TwoIntDecimal Ceil(TwoIntDecimal dec) => RoundAtDecimalPoint(dec, RoundingMode.Up);

    internal static TwoIntDecimal Round(TwoIntDecimal dec) =>
        RoundAtDecimalPoint(dec, RoundingMode.HalfAwayFromZero);

    /* Rounds to a number of digits after the decimal point. Shifting by a power of ten is exact,
     * so this is the integer rounding above applied at a moved decimal point. */
    internal static TwoIntDecimal Round(TwoIntDecimal dec, int decimalPlaces)
    {
        if (!IsFinite(dec) || (dec.Mantissa == 0))
        {
            return dec;
        }
        if (decimalPlaces >= MANTISSA_DIGIT_COUNT)
        {
            return dec;
        }

        TwoIntDecimal Shifted = ScaleByPowerOfTen(dec, decimalPlaces);
        return ScaleByPowerOfTen(Round(Shifted), -decimalPlaces);
    }

    /* Ordering. Returns -1, 0 or 1 and must not be handed a NaN, which has no place in an order. */
    internal static int Compare(TwoIntDecimal a, TwoIntDecimal b)
    {
        int SignA = Sign(a);
        int SignB = Sign(b);

        if (SignA != SignB)
        {
            return SignA < SignB ? -1 : 1;
        }
        if (SignA == 0)
        {
            return 0;
        }
        if (a.Exponent != b.Exponent)
        {
            /* A larger exponent means a larger magnitude, which for two negative values means the
             * smaller number. */
            return (a.Exponent < b.Exponent) == (SignA > 0) ? -1 : 1;
        }
        if (a.Mantissa != b.Mantissa)
        {
            return a.Mantissa < b.Mantissa ? -1 : 1;
        }
        return 0;
    }

    /* Text. This is the one parser for decimal text, and that includes the literals in KGVL source:
     * the source parser only finds where a literal starts and ends and hands its text over, so what
     * counts as a valid decimal is decided here and nowhere else. */
    internal static bool TryParse(string number, out TwoIntDecimal dec)
    {
        return TryParse(number, out dec, out _);
    }

    /* Reports what was wrong and where instead of throwing, so that the compiler can queue a
     * malformed literal as an error and carry on parsing. */
    internal static bool TryParse(string number, out TwoIntDecimal dec, out DecimalParseError error)
    {
        dec = Zero;
        error = default;

        if (string.IsNullOrWhiteSpace(number))
        {
            error = new(DecimalParseErrorKind.Empty, 0);
            return false;
        }

        /* Trimmed by index rather than with Trim, so that an error index still points into the text
         * the caller holds. */
        int StartIndex = 0;
        int EndIndex = number.Length;
        while (char.IsWhiteSpace(number[StartIndex]))
        {
            StartIndex++;
        }
        while (char.IsWhiteSpace(number[EndIndex - 1]))
        {
            EndIndex--;
        }

        string Text = number.Substring(StartIndex, EndIndex - StartIndex);
        if (string.Equals(Text, NAN_TEXT, StringComparison.OrdinalIgnoreCase))
        {
            dec = NaN;
            return true;
        }
        if (string.Equals(Text, POSITIVE_INFINITY_TEXT, StringComparison.OrdinalIgnoreCase))
        {
            dec = PositiveInfinity;
            return true;
        }
        if (string.Equals(Text, NEGATIVE_INFINITY_TEXT, StringComparison.OrdinalIgnoreCase))
        {
            dec = NegativeInfinity;
            return true;
        }

        return TryParseNumber(number, StartIndex, EndIndex, out dec, out error);
    }

    internal static TwoIntDecimal Parse(string number)
    {
        ArgumentNullException.ThrowIfNull(number, nameof(number));

        if (!TryParse(number, out TwoIntDecimal Result, out DecimalParseError Error))
        {
            throw new FormatException($"\"{number}\" is not a valid decimal number: {Error}.");
        }
        return Result;
    }


    // Private static methods.
    /* Primitives. */
    /* The magnitude of a mantissa. int.MinValue has no positive counterpart, so it saturates; it is
     * outside the mantissa range and therefore already a NaN encoding by the time it gets here. */
    private static int GetMagnitude(int value)
    {
        if (value == int.MinValue)
        {
            return int.MaxValue;
        }
        return value < 0 ? -value : value;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }
        if (value > max)
        {
            return max;
        }
        return value;
    }

    /* Floor division by a positive divisor. C# truncates towards zero, a scoreboard floors, and
     * this is the shape both agree on. */
    private static int FloorDivide(int value, int divisor)
    {
        if (value >= 0)
        {
            return value / divisor;
        }
        return -((divisor - 1 - value) / divisor);
    }

    /* Produces the next decimal digit of remainder / divisor and leaves the remainder of the
     * following digit position behind. The tenfold scaling of the remainder would need 34 bits, so
     * it is carried as a high part in [0, 9] and a low part below MANTISSA_LIMIT, and the digit is
     * found by subtracting the divisor at most nine times. Both arguments stay non-negative and the
     * remainder stays below the divisor. */
    private static int TakeNextQuotientDigit(int divisor, ref int remainder)
    {
        int ScaledHigh = remainder / MIN_MANTISSA;
        int ScaledLow = (remainder % MIN_MANTISSA) * 10;
        int Digit = 0;

        while ((ScaledHigh > 0) || (ScaledLow >= divisor))
        {
            if (ScaledLow >= divisor)
            {
                ScaledLow -= divisor;
            }
            else
            {
                ScaledHigh--;
                ScaledLow += MANTISSA_LIMIT - divisor;
            }
            Digit++;
        }

        remainder = ScaledLow;
        return Digit;
    }

    /* Arithmetic. */
    private static TwoIntDecimal Add(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (IsNaN(a) || IsNaN(b))
        {
            return NaN;
        }
        if (IsInfinity(a))
        {
            if (IsInfinity(b) && (Sign(a) != Sign(b)))
            {
                return NaN;
            }
            return a;
        }
        if (IsInfinity(b))
        {
            return b;
        }
        if (a.Mantissa == 0)
        {
            return b;
        }
        if (b.Mantissa == 0)
        {
            return a;
        }

        TwoIntDecimal High = a;
        TwoIntDecimal Low = b;
        if (b.Exponent > a.Exponent)
        {
            High = b;
            Low = a;
        }

        /* Both exponents are inside the usable range, so the difference cannot wrap. Past nine
         * digits of separation the smaller value cannot reach even half of the larger one's last
         * digit, so it changes nothing. */
        int ExponentDifference = High.Exponent - Low.Exponent;
        if (ExponentDifference > MANTISSA_DIGIT_COUNT)
        {
            return High;
        }

        int HighMagnitude = GetMagnitude(High.Mantissa);
        int LowMagnitude = GetMagnitude(Low.Mantissa);
        bool IsSameSign = (High.Mantissa < 0) == (Low.Mantissa < 0);
        int ResultSign = High.Mantissa < 0 ? -1 : 1;
        int ResultExponent = High.Exponent;

        /* Value is the result in units of the high operand's last mantissa digit, and what the
         * alignment shifted off the low operand is kept exactly as Remainder / Divisor so that a
         * cancelling subtraction can shift those digits back in. Both sums stay below 2e9. */
        int Value;
        int Remainder = 0;
        int Divisor = 1;

        if (ExponentDifference == 0)
        {
            Value = IsSameSign ? (HighMagnitude + LowMagnitude) : (HighMagnitude - LowMagnitude);
            if (Value == 0)
            {
                return Zero;
            }
            if (Value < 0)
            {
                Value = -Value;
                ResultSign = -ResultSign;
            }
        }
        else
        {
            Divisor = POWERS_OF_TEN[ExponentDifference];
            int AlignedLow = LowMagnitude / Divisor;
            Remainder = LowMagnitude % Divisor;

            if (IsSameSign)
            {
                Value = HighMagnitude + AlignedLow;
            }
            else
            {
                /* AlignedLow is below MIN_MANTISSA and HighMagnitude is at least MIN_MANTISSA, so
                 * the difference cannot go negative and the sign cannot flip. */
                Value = HighMagnitude - AlignedLow;
                if (Remainder != 0)
                {
                    Value--;
                    Remainder = Divisor - Remainder;
                }
            }
        }

        if (Value > MAX_MANTISSA)
        {
            /* Only a same-sign addition can carry, and it can carry by at most one digit. */
            int DroppedDigit = Value % 10;
            Value /= 10;
            ResultExponent++;
            if (DroppedDigit >= 5)
            {
                Value++;
            }
            return FromComponents(ResultSign * Value, ResultExponent);
        }

        while (Value < MIN_MANTISSA)
        {
            int NextDigit = 0;
            if (Divisor > 1)
            {
                Divisor /= 10;
                NextDigit = Remainder / Divisor;
                Remainder %= Divisor;
            }
            Value = (Value * 10) + NextDigit;
            ResultExponent--;
        }

        if (Divisor > 1)
        {
            int RoundDigit = Remainder / (Divisor / 10);
            if (RoundDigit >= 5)
            {
                Value++;
            }
        }
        return FromComponents(ResultSign * Value, ResultExponent);
    }

    /* Multiplies two nine digit mantissas. The exact product needs eighteen digits, so it is built
     * from three digit limbs whose partial products all stay below 1e7, then split into a high and
     * a low nine digit half from which the nine significant digits and a rounding digit are read.
     * Nothing here exceeds 32 bits. */
    private static TwoIntDecimal Multiply(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (IsNaN(a) || IsNaN(b))
        {
            return NaN;
        }

        int ResultSign = ((a.Mantissa < 0) == (b.Mantissa < 0)) ? 1 : -1;

        if (IsInfinity(a) || IsInfinity(b))
        {
            if ((a.Mantissa == 0) || (b.Mantissa == 0))
            {
                return NaN;
            }
            return ResultSign < 0 ? NegativeInfinity : PositiveInfinity;
        }
        if ((a.Mantissa == 0) || (b.Mantissa == 0))
        {
            return Zero;
        }

        int LeftMagnitude = GetMagnitude(a.Mantissa);
        int RightMagnitude = GetMagnitude(b.Mantissa);

        int LeftLow = LeftMagnitude % 1_000;
        int LeftMiddle = (LeftMagnitude / 1_000) % 1_000;
        int LeftHigh = LeftMagnitude / 1_000_000;
        int RightLow = RightMagnitude % 1_000;
        int RightMiddle = (RightMagnitude / 1_000) % 1_000;
        int RightHigh = RightMagnitude / 1_000_000;

        int Column0 = LeftLow * RightLow;
        int Column1 = (LeftLow * RightMiddle) + (LeftMiddle * RightLow);
        int Column2 = (LeftLow * RightHigh) + (LeftMiddle * RightMiddle) + (LeftHigh * RightLow);
        int Column3 = (LeftMiddle * RightHigh) + (LeftHigh * RightMiddle);
        int Column4 = LeftHigh * RightHigh;

        int Limb0 = Column0 % 1_000;
        int Carry = Column0 / 1_000;
        Column1 += Carry;
        int Limb1 = Column1 % 1_000;
        Carry = Column1 / 1_000;
        Column2 += Carry;
        int Limb2 = Column2 % 1_000;
        Carry = Column2 / 1_000;
        Column3 += Carry;
        int Limb3 = Column3 % 1_000;
        Carry = Column3 / 1_000;
        Column4 += Carry;
        int Limb4 = Column4 % 1_000;
        int Limb5 = Column4 / 1_000;

        int ProductLow = (Limb2 * 1_000_000) + (Limb1 * 1_000) + Limb0;
        int ProductHigh = (Limb5 * 1_000_000) + (Limb4 * 1_000) + Limb3;

        /* The product of two values in [1e8, 1e9) lies in [1e16, 1e18), so it is seventeen or
         * eighteen digits long and the nine digit result is one shift away in either case. */
        int ResultMantissa;
        int RoundDigit;
        int ResultExponent = a.Exponent + b.Exponent;

        if (ProductHigh >= MIN_MANTISSA)
        {
            ResultMantissa = ProductHigh;
            RoundDigit = ProductLow / MIN_MANTISSA;
            ResultExponent++;
        }
        else
        {
            ResultMantissa = (ProductHigh * 10) + (ProductLow / MIN_MANTISSA);
            RoundDigit = (ProductLow % MIN_MANTISSA) / 10_000_000;
        }

        if (RoundDigit >= 5)
        {
            ResultMantissa++;
        }
        return FromComponents(ResultSign * ResultMantissa, ResultExponent);
    }

    /* Long division, one decimal digit at a time, exactly the way the backend has to emit it. */
    private static TwoIntDecimal Divide(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (IsNaN(a) || IsNaN(b))
        {
            return NaN;
        }

        int ResultSign = ((a.Mantissa < 0) == (b.Mantissa < 0)) ? 1 : -1;

        if (IsInfinity(a))
        {
            if (IsInfinity(b))
            {
                return NaN;
            }
            return ResultSign < 0 ? NegativeInfinity : PositiveInfinity;
        }
        if (IsInfinity(b))
        {
            return Zero;
        }
        if (b.Mantissa == 0)
        {
            if (a.Mantissa == 0)
            {
                return NaN;
            }
            return ResultSign < 0 ? NegativeInfinity : PositiveInfinity;
        }
        if (a.Mantissa == 0)
        {
            return Zero;
        }

        int Numerator = GetMagnitude(a.Mantissa);
        int Denominator = GetMagnitude(b.Mantissa);
        int ResultExponent = a.Exponent - b.Exponent;

        int Quotient;
        int Remainder;
        if (Numerator >= Denominator)
        {
            Quotient = Numerator / Denominator;
            Remainder = Numerator % Denominator;
        }
        else
        {
            /* The quotient starts below one, so its first significant digit is one position lower. */
            Quotient = 0;
            Remainder = Numerator;
            ResultExponent--;
        }

        while (Quotient < MIN_MANTISSA)
        {
            Quotient = (Quotient * 10) + TakeNextQuotientDigit(Denominator, ref Remainder);
        }

        if (TakeNextQuotientDigit(Denominator, ref Remainder) >= 5)
        {
            Quotient++;
        }
        return FromComponents(ResultSign * Quotient, ResultExponent);
    }

    /* Rounding helpers. */
    /* The number of mantissa digits that sit after the decimal point. */
    private static int GetFractionDigitCount(TwoIntDecimal dec)
    {
        return Clamp(MANTISSA_EXPONENT - dec.Exponent, 0, MANTISSA_DIGIT_COUNT);
    }

    /* The mantissa digits after the decimal point, as a non-negative number. */
    private static int GetFractionDigits(TwoIntDecimal dec)
    {
        return GetMagnitude(dec.Mantissa) % POWERS_OF_TEN[GetFractionDigitCount(dec)];
    }

    private static TwoIntDecimal RoundAtDecimalPoint(TwoIntDecimal dec, RoundingMode mode)
    {
        if (!IsFinite(dec) || (dec.Mantissa == 0))
        {
            return dec;
        }

        int Sign = dec.Mantissa < 0 ? -1 : 1;

        /* One step at the integer position is 10^FractionDigitCount in mantissa units. Once that
         * passes the width of the mantissa the value is below a tenth, so its integer part is zero
         * and a step can only ever be one whole unit. */
        int FractionDigitCount = MANTISSA_EXPONENT - dec.Exponent;
        if (FractionDigitCount <= 0)
        {
            return dec;
        }
        if (FractionDigitCount > MANTISSA_DIGIT_COUNT)
        {
            bool IsWholeStepNeeded = mode switch
            {
                RoundingMode.Down => Sign < 0,
                RoundingMode.Up => Sign > 0,
                /* Anything below a tenth is well short of the halfway point. */
                _ => false
            };

            if (!IsWholeStepNeeded)
            {
                return Zero;
            }
            return Sign < 0 ? -One : One;
        }

        int Scale = POWERS_OF_TEN[FractionDigitCount];
        int Magnitude = GetMagnitude(dec.Mantissa);
        int Dropped = Magnitude % Scale;
        int Kept = Magnitude - Dropped;

        if (Dropped != 0)
        {
            bool IsStepNeeded = mode switch
            {
                RoundingMode.TowardsZero => false,
                RoundingMode.Down => Sign < 0,
                RoundingMode.Up => Sign > 0,
                /* Scale is at most 1e9, so doubling the dropped digits stays inside 32 bits. */
                RoundingMode.HalfAwayFromZero => (Dropped * 2) >= Scale,
                _ => false
            };

            if (IsStepNeeded)
            {
                /* Kept + Scale can pass MAX_MANTISSA, which FromComponents normalises away. */
                Kept += Scale;
            }
        }

        return FromComponents(Sign * Kept, dec.Exponent);
    }

    /* Conversion. */
    private static TwoIntDecimal FromInt32(int value)
    {
        if (value == 0)
        {
            return Zero;
        }
        if (value == int.MinValue)
        {
            /* int.MinValue has no positive counterpart, and its nine digit rounding is a constant. */
            return new(-214_748_365, 9);
        }

        int Sign = value < 0 ? -1 : 1;
        int Magnitude = value < 0 ? -value : value;
        int Exponent = 0;

        /* An int is at most ten digits long, so at most one digit is ever dropped. */
        if (Magnitude > MAX_MANTISSA)
        {
            int DroppedDigit = Magnitude % 10;
            Magnitude /= 10;
            Exponent++;
            if (DroppedDigit >= 5)
            {
                Magnitude++;
            }
        }
        while (Magnitude < MIN_MANTISSA)
        {
            Magnitude *= 10;
            Exponent--;
        }

        return new(Sign * Magnitude, Exponent + MANTISSA_EXPONENT);
    }

    /* KGVL's 64 bit integers do not exist on the target yet -- they will have to be synthesised
     * from pairs of scoreboard values just as this type is -- so the two long conversions run on
     * the host only, and the backend will need its own lowering once that type exists. */
    private static TwoIntDecimal FromInt64(long value)
    {
        if (value == 0L)
        {
            return Zero;
        }

        int Sign = value < 0L ? -1 : 1;
        ulong Magnitude = value < 0L ? ((ulong)(-(value + 1L)) + 1UL) : (ulong)value;
        int Exponent = 0;

        ulong DroppedDigit = 0UL;
        while (Magnitude > MAX_MANTISSA)
        {
            DroppedDigit = Magnitude % 10UL;
            Magnitude /= 10UL;
            Exponent++;
        }
        if (DroppedDigit >= 5UL)
        {
            Magnitude++;
        }
        while (Magnitude < MIN_MANTISSA)
        {
            Magnitude *= 10UL;
            Exponent--;
        }

        return FromComponents(Sign * (int)Magnitude, Exponent + MANTISSA_EXPONENT);
    }

    private static long ToInt64(TwoIntDecimal dec)
    {
        if (!IsFinite(dec) || (dec.Mantissa == 0))
        {
            return 0L;
        }

        TwoIntDecimal Truncated = Truncate(dec);
        if (Truncated.Mantissa == 0)
        {
            return 0L;
        }
        if (Truncated.Exponent > 18)
        {
            return Truncated.Mantissa < 0 ? long.MinValue : long.MaxValue;
        }

        int Sign = Truncated.Mantissa < 0 ? -1 : 1;
        long Magnitude = GetMagnitude(Truncated.Mantissa);
        int Shift = Truncated.Exponent - MANTISSA_EXPONENT;

        if (Shift < 0)
        {
            Magnitude /= POWERS_OF_TEN[-Shift];
        }
        else
        {
            for (int i = 0; i < Shift; i++)
            {
                if (Magnitude > (long.MaxValue / 10L))
                {
                    return Sign < 0 ? long.MinValue : long.MaxValue;
                }
                Magnitude *= 10L;
            }
        }
        return Sign * Magnitude;
    }

    /* Truncates towards zero into an int, saturating at the int range. Only the mantissa digits
     * that sit before the decimal point survive, so the shift never exceeds nine positions. */
    private static int ToInt32(TwoIntDecimal dec)
    {
        if (!IsFinite(dec) || (dec.Mantissa == 0))
        {
            return 0;
        }

        TwoIntDecimal Truncated = Truncate(dec);
        if (Truncated.Mantissa == 0)
        {
            return 0;
        }
        if (Truncated.Exponent > 9)
        {
            return Truncated.Mantissa < 0 ? int.MinValue : int.MaxValue;
        }

        int Sign = Truncated.Mantissa < 0 ? -1 : 1;
        int Magnitude = GetMagnitude(Truncated.Mantissa);
        int Shift = Truncated.Exponent - MANTISSA_EXPONENT;

        if (Shift < 0)
        {
            Magnitude /= POWERS_OF_TEN[-Shift];
        }
        else
        {
            for (int i = 0; i < Shift; i++)
            {
                if (Magnitude > (int.MaxValue / 10))
                {
                    return Sign < 0 ? int.MinValue : int.MaxValue;
                }
                Magnitude *= 10;
            }
        }
        return Sign * Magnitude;
    }

    /* Text helpers. Compiler side only. */
    /* Parses the characters from startIndex up to endIndex. Error indices are into the whole text. */
    private static bool TryParseNumber(string text, int startIndex, int endIndex, out TwoIntDecimal dec,
        out DecimalParseError error)
    {
        dec = Zero;
        error = default;

        int Index = startIndex;
        bool IsNegative = false;
        if ((text[Index] == NEGATION_SYMBOL) || (text[Index] == POSITIVE_SYMBOL))
        {
            IsNegative = text[Index] == NEGATION_SYMBOL;
            Index++;
        }

        int Mantissa = 0;
        int TakenDigitCount = 0;
        int RoundDigit = 0;
        int DigitStreamIndex = 0;
        int IntegerDigitCount = 0;
        int FirstSignificantIndex = 0;
        bool HasFoundSignificantDigit = false;
        bool HasSeparator = false;
        bool HasAnyDigit = false;

        while ((Index < endIndex) && !IsExponentSymbol(text[Index]))
        {
            char Character = text[Index];

            if (char.IsAsciiDigit(Character))
            {
                int Digit = Character - '0';
                HasAnyDigit = true;

                if (!HasSeparator)
                {
                    IntegerDigitCount++;
                }
                if (!HasFoundSignificantDigit && (Digit != 0))
                {
                    HasFoundSignificantDigit = true;
                    FirstSignificantIndex = DigitStreamIndex;
                }
                if (HasFoundSignificantDigit)
                {
                    if (TakenDigitCount < MANTISSA_DIGIT_COUNT)
                    {
                        Mantissa = (Mantissa * 10) + Digit;
                        TakenDigitCount++;
                    }
                    else if (TakenDigitCount == MANTISSA_DIGIT_COUNT)
                    {
                        RoundDigit = Digit;
                        TakenDigitCount++;
                    }
                }
                DigitStreamIndex++;
            }
            else if ((Character == DECIMAL_SEPARATOR_PERIOD) || (Character == DECIMAL_SEPARATOR_COMMA))
            {
                if (HasSeparator)
                {
                    error = new(DecimalParseErrorKind.MultipleSeparators, Index);
                    return false;
                }
                HasSeparator = true;
            }
            else
            {
                error = new(DecimalParseErrorKind.UnexpectedCharacter, Index);
                return false;
            }
            Index++;
        }

        if (!HasAnyDigit)
        {
            error = new(DecimalParseErrorKind.MissingDigits, Index);
            return false;
        }

        /* The loop stopped either at the end or on the exponent symbol, which is skipped. */
        long WrittenExponent = 0L;
        if ((Index < endIndex)
            && !TryParseExponent(text, Index + 1, endIndex, out WrittenExponent, out error))
        {
            return false;
        }

        if (!HasFoundSignificantDigit)
        {
            dec = Zero;
            return true;
        }

        while (TakenDigitCount < MANTISSA_DIGIT_COUNT)
        {
            Mantissa *= 10;
            TakenDigitCount++;
        }
        if (RoundDigit >= 5)
        {
            Mantissa++;
        }

        /* The digit stream index of the first significant digit measures how far that digit sits
         * from the last digit before the decimal point. A long literal could push this past the
         * usable exponent range, which FromComponents saturates. */
        long Exponent = (IntegerDigitCount - 1L) - FirstSignificantIndex + WrittenExponent;
        int SaturatedExponent = (int)Math.Clamp(Exponent, MIN_EXPONENT - 1L, MAX_EXPONENT + 1L);

        dec = FromComponents(IsNegative ? -Mantissa : Mantissa, SaturatedExponent);
        return true;
    }

    /* Parses the exponent's sign and digits, from just after the exponent symbol up to endIndex. */
    private static bool TryParseExponent(string text, int startIndex, int endIndex, out long exponent,
        out DecimalParseError error)
    {
        exponent = 0L;
        error = default;

        int Index = startIndex;
        bool IsNegative = false;
        if ((Index < endIndex) && ((text[Index] == NEGATION_SYMBOL) || (text[Index] == POSITIVE_SYMBOL)))
        {
            IsNegative = text[Index] == NEGATION_SYMBOL;
            Index++;
        }
        if ((Index >= endIndex) || !char.IsAsciiDigit(text[Index]))
        {
            error = new(DecimalParseErrorKind.MissingExponentDigits, Index);
            return false;
        }

        long Value = 0L;
        while (Index < endIndex)
        {
            if (!char.IsAsciiDigit(text[Index]))
            {
                error = new(DecimalParseErrorKind.UnexpectedCharacter, Index);
                return false;
            }
            /* Anything past the usable exponent range saturates anyway, so it stops accumulating. */
            if (Value <= MAX_EXPONENT)
            {
                Value = (Value * 10L) + (text[Index] - '0');
            }
            Index++;
        }

        exponent = IsNegative ? -Value : Value;
        return true;
    }

    private static bool IsExponentSymbol(char character)
    {
        return char.ToLowerInvariant(character) == EXPONENT_SYMBOL;
    }

    private static string TrimTrailingZeros(string digits)
    {
        int Length = digits.Length;
        while ((Length > 0) && (digits[Length - 1] == '0'))
        {
            Length--;
        }
        return digits.Substring(0, Length);
    }

    private static string GetScientificNotationString(TwoIntDecimal dec, string digits)
    {
        StringBuilder Builder = new();

        if (dec.Mantissa < 0)
        {
            Builder.Append(NEGATION_SYMBOL);
        }
        Builder.Append(digits[0]);

        string Fraction = TrimTrailingZeros(digits.Substring(1));
        if (Fraction.Length > 0)
        {
            Builder.Append(DECIMAL_SEPARATOR_PERIOD);
            Builder.Append(Fraction);
        }

        Builder.Append(EXPONENT_SYMBOL);
        Builder.Append(dec.Exponent.ToString(CultureInfo.InvariantCulture));
        return Builder.ToString();
    }

    private static string GetRegularString(TwoIntDecimal dec, string digits)
    {
        StringBuilder Builder = new();

        if (dec.Mantissa < 0)
        {
            Builder.Append(NEGATION_SYMBOL);
        }

        if (dec.Exponent < 0)
        {
            Builder.Append('0');
            Builder.Append(DECIMAL_SEPARATOR_PERIOD);
            for (int i = dec.Exponent + 1; i < 0; i++)
            {
                Builder.Append('0');
            }
            Builder.Append(TrimTrailingZeros(digits));
            return Builder.ToString();
        }

        if (dec.Exponent >= MANTISSA_EXPONENT)
        {
            Builder.Append(digits);
            for (int i = MANTISSA_EXPONENT; i < dec.Exponent; i++)
            {
                Builder.Append('0');
            }
            return Builder.ToString();
        }

        Builder.Append(digits.Substring(0, dec.Exponent + 1));
        string Fraction = TrimTrailingZeros(digits.Substring(dec.Exponent + 1));
        if (Fraction.Length > 0)
        {
            Builder.Append(DECIMAL_SEPARATOR_PERIOD);
            Builder.Append(Fraction);
        }
        return Builder.ToString();
    }


    // Inherited methods.
    public override string ToString()
    {
        if (IsNaN(this))
        {
            return NAN_TEXT;
        }
        if (IsInfinity(this))
        {
            return Mantissa < 0 ? NEGATIVE_INFINITY_TEXT : POSITIVE_INFINITY_TEXT;
        }
        if (Mantissa == 0)
        {
            return "0";
        }

        string Digits = GetMagnitude(Mantissa).ToString("D9", CultureInfo.InvariantCulture);

        const int SCIENTIFIC_NOTATION_MAX_EXPONENT = 17;
        const int SCIENTIFIC_NOTATION_MIN_EXPONENT = -9;
        if ((Exponent > SCIENTIFIC_NOTATION_MAX_EXPONENT) || (Exponent < SCIENTIFIC_NOTATION_MIN_EXPONENT))
        {
            return GetScientificNotationString(this, Digits);
        }
        return GetRegularString(this, Digits);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is TwoIntDecimal Other)
        {
            /* Unlike the == operator this is reflexive even for NaN, because a value has to be able
             * to find itself again in a dictionary. */
            return (Mantissa == Other.Mantissa) && (Exponent == Other.Exponent);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Mantissa, Exponent);
    }


    // Operators.
    public static bool operator ==(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (IsNaN(a) || IsNaN(b))
        {
            return false;
        }
        return (a.Mantissa == b.Mantissa) && (a.Exponent == b.Exponent);
    }

    public static bool operator !=(TwoIntDecimal a, TwoIntDecimal b)
    {
        return !(a == b);
    }

    public static bool operator >(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (IsNaN(a) || IsNaN(b))
        {
            return false;
        }
        return Compare(a, b) > 0;
    }

    public static bool operator <(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (IsNaN(a) || IsNaN(b))
        {
            return false;
        }
        return Compare(a, b) < 0;
    }

    public static bool operator >=(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (IsNaN(a) || IsNaN(b))
        {
            return false;
        }
        return Compare(a, b) >= 0;
    }

    public static bool operator <=(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (IsNaN(a) || IsNaN(b))
        {
            return false;
        }
        return Compare(a, b) <= 0;
    }

    public static TwoIntDecimal operator +(TwoIntDecimal a) => a;

    public static TwoIntDecimal operator -(TwoIntDecimal a)
    {
        if (IsNaN(a) || (a.Mantissa == 0))
        {
            return a;
        }
        return new(-a.Mantissa, a.Exponent);
    }

    public static TwoIntDecimal operator +(TwoIntDecimal a, TwoIntDecimal b) => Add(a, b);

    public static TwoIntDecimal operator -(TwoIntDecimal a, TwoIntDecimal b) => Add(a, -b);

    public static TwoIntDecimal operator *(TwoIntDecimal a, TwoIntDecimal b) => Multiply(a, b);

    public static TwoIntDecimal operator /(TwoIntDecimal a, TwoIntDecimal b) => Divide(a, b);

    /* The truncated remainder, as a - b * Truncate(a / b). This is not exact. The quotient and the
     * product are each rounded to nine digits, so whenever b times the whole quotient needs more
     * digits than that, the low digits of the remainder are lost; and a quotient that rounds up across
     * a whole number breaks it outright, so 8.99999999 % 3 gives -0.00000001 instead of 2.99999999.
     * It needs replacing with a remainder computed digit by digit, which can be exact. */
    public static TwoIntDecimal operator %(TwoIntDecimal a, TwoIntDecimal b)
    {
        if (IsNaN(a) || IsNaN(b) || IsInfinity(a) || (b.Mantissa == 0))
        {
            return NaN;
        }
        if (IsInfinity(b) || (a.Mantissa == 0))
        {
            return a;
        }

        return Add(a, -Multiply(b, Truncate(Divide(a, b))));
    }

    public static TwoIntDecimal operator ++(TwoIntDecimal dec) => Add(dec, One);

    public static TwoIntDecimal operator --(TwoIntDecimal dec) => Add(dec, -One);

    public static implicit operator TwoIntDecimal(byte number) => FromInt32(number);

    public static implicit operator TwoIntDecimal(short number) => FromInt32(number);

    public static implicit operator TwoIntDecimal(int number) => FromInt32(number);

    public static explicit operator int(TwoIntDecimal dec) => ToInt32(dec);

    public static explicit operator short(TwoIntDecimal dec) => (short)ToInt32(dec);

    public static explicit operator byte(TwoIntDecimal dec) => (byte)ToInt32(dec);

    /* Host side conversions. These exist so that the compiler and its tests can move values in and
     * out of the type; a datapack has no double and the backend must never emit them. Both go
     * through the decimal text form rather than through binary floating point arithmetic, so no
     * base-2 rounding is introduced on the way. */
    public static explicit operator TwoIntDecimal(double number)
    {
        if (double.IsNaN(number))
        {
            return NaN;
        }
        if (double.IsPositiveInfinity(number))
        {
            return PositiveInfinity;
        }
        if (double.IsNegativeInfinity(number))
        {
            return NegativeInfinity;
        }

        /* The round-trip text of a finite double is always a valid decimal, so this never throws. */
        return Parse(number.ToString("R", CultureInfo.InvariantCulture));
    }

    public static explicit operator double(TwoIntDecimal dec)
    {
        if (IsNaN(dec))
        {
            return double.NaN;
        }
        if (IsInfinity(dec))
        {
            return dec.Mantissa < 0 ? double.NegativeInfinity : double.PositiveInfinity;
        }
        if (dec.Mantissa == 0)
        {
            return 0d;
        }

        string Digits = GetMagnitude(dec.Mantissa).ToString("D9", CultureInfo.InvariantCulture);
        return double.Parse(GetScientificNotationString(dec, Digits), NumberStyles.Float,
            CultureInfo.InvariantCulture);
    }

    public static implicit operator TwoIntDecimal(long number) => FromInt64(number);

    public static explicit operator long(TwoIntDecimal dec) => ToInt64(dec);

    public static explicit operator float(TwoIntDecimal dec) => (float)(double)dec;


    // Types.
    private enum RoundingMode
    {
        TowardsZero,
        Down,
        Up,
        HalfAwayFromZero
    }
}
