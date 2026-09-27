namespace KeigValCompiler.Semantician;

/* The mathematical functions over TwoIntDecimal.
 *
 * TwoIntDecimal itself holds the number and the operations that are exact: the four arithmetic
 * operators, comparison, and the rounding family. Everything here approximates, and everything here
 * is built out of those operators alone, so the datapack backend can emit these algorithms as
 * commands the same way it emits the arithmetic beneath them. No double, no Math.* helper, no
 * intermediate wider than 32 bits.
 *
 * Every algorithm whose accuracy grows with the amount of work it does takes an iteration count, so
 * that the standard library can expose the accuracy-for-commands trade to KGVL code. Each one also
 * has an overload that uses the default below, which is chosen to reach the nine digits the format
 * can hold. Raising an iteration count past the point where the format saturates costs commands and
 * buys nothing.
 *
 * Accuracy that the format itself limits, rather than the iteration count:
 *
 *   - The trigonometric functions reduce their argument against a nine digit pi/2, so they lose
 *     roughly one digit for every power of ten in |x|. Past 1e9 nothing is left and they return
 *     NaN rather than a number with no correct digits in it. A libm backed by a multi-hundred digit
 *     pi does better; this format cannot hold one.
 *   - Exp and Pow reduce through a nine digit logarithm, so a large exponent loses low digits the
 *     same way.
 *
 * As in TwoIntDecimal, nothing here throws. Undefined results are NaN and out-of-range results are
 * an infinity or zero. */
internal static class TwoIntDecimalMath
{
    // Internal static fields.
    /* Default iteration counts. Newton's method roughly doubles the number of correct digits per
     * step, the power and logarithm series add a little under one digit per term, and CORDIC adds
     * about one bit per step, so nine decimal digits needs about thirty of them. */
    internal const int DEFAULT_NEWTON_ITERATIONS = 8;
    internal const int DEFAULT_SERIES_ITERATIONS = 24;
    internal const int DEFAULT_CORDIC_ITERATIONS = 32;

    internal const int MIN_CORDIC_ITERATIONS = 2;
    internal const int MAX_CORDIC_ITERATIONS = 32;
    internal const int MIN_SERIES_ITERATIONS = 1;
    internal const int MAX_SERIES_ITERATIONS = 1_000;
    internal const int MIN_NEWTON_ITERATIONS = 1;
    internal const int MAX_NEWTON_ITERATIONS = 1_000;


    // Private static fields.
    /* Constants. */
    private static readonly TwoIntDecimal HALF_PI = TwoIntDecimal.FromComponents(157_079_633, 0);
    private static readonly TwoIntDecimal THREE = TwoIntDecimal.FromComponents(300_000_000, 0);
    private static readonly TwoIntDecimal LN_10 = TwoIntDecimal.FromComponents(230_258_509, 0);
    private static readonly TwoIntDecimal LOG10_E = TwoIntDecimal.FromComponents(434_294_482, -1);
    private static readonly TwoIntDecimal LOG10_2 = TwoIntDecimal.FromComponents(301_029_996, -1);
    private static readonly TwoIntDecimal SQRT_10 = TwoIntDecimal.FromComponents(316_227_766, 0);
    private static readonly TwoIntDecimal DEGREES_TO_RADIANS = TwoIntDecimal.FromComponents(174_532_925, -2);
    private static readonly TwoIntDecimal RADIANS_TO_DEGREES = TwoIntDecimal.FromComponents(572_957_795, 1);

    /* Past this the hyperbolic tangent is one to every digit the format has. */
    private static readonly TwoIntDecimal TANH_SATURATION_POINT =
        TwoIntDecimal.FromComponents(120_000_000, 1);

    /* Past this an argument no longer has a single correct digit left after being reduced against a
     * nine digit pi/2, so the trigonometric functions refuse it. */
    private static readonly TwoIntDecimal TRIGONOMETRY_ARGUMENT_LIMIT =
        TwoIntDecimal.FromComponents(100_000_000, 9);

    /* Square and cube root seeds, one per residue of the exponent. Taking the geometric middle of
     * the interval each residue covers keeps the initial relative error under a factor of two, which
     * is four Newton steps away from the full nine digits. */
    private static readonly int[] SQUARE_ROOT_SEEDS = new int[] { 177_827_941, 562_341_325 };
    private static readonly int[] CUBE_ROOT_SEEDS = new int[] { 146_779_927, 316_227_766, 681_292_069 };

    /* CORDIC tables. CORDIC_ANGLES[i] is atan(2^-i), CORDIC_SCALES[i] is 2^-i, and
     * CORDIC_INVERSE_GAINS[n - 1] is the reciprocal of the gain that n rotations introduce, so that
     * a rotation can be seeded with it instead of dividing the result afterwards. All three are
     * hardcoded rather than derived, because deriving 2^-i by repeated halving would round at every
     * step and this format has no bits to spare. */
    private static readonly TwoIntDecimal[] CORDIC_ANGLES = new TwoIntDecimal[]
    {
        TwoIntDecimal.FromComponents(785398163, -1), TwoIntDecimal.FromComponents(463647609, -1),
        TwoIntDecimal.FromComponents(244978663, -1), TwoIntDecimal.FromComponents(124354995, -1),
        TwoIntDecimal.FromComponents(624188100, -2), TwoIntDecimal.FromComponents(312398334, -2),
        TwoIntDecimal.FromComponents(156237286, -2), TwoIntDecimal.FromComponents(781234106, -3),
        TwoIntDecimal.FromComponents(390623013, -3), TwoIntDecimal.FromComponents(195312252, -3),
        TwoIntDecimal.FromComponents(976562190, -4), TwoIntDecimal.FromComponents(488281211, -4),
        TwoIntDecimal.FromComponents(244140620, -4), TwoIntDecimal.FromComponents(122070312, -4),
        TwoIntDecimal.FromComponents(610351562, -5), TwoIntDecimal.FromComponents(305175781, -5),
        TwoIntDecimal.FromComponents(152587891, -5), TwoIntDecimal.FromComponents(762939453, -6),
        TwoIntDecimal.FromComponents(381469727, -6), TwoIntDecimal.FromComponents(190734863, -6),
        TwoIntDecimal.FromComponents(953674316, -7), TwoIntDecimal.FromComponents(476837158, -7),
        TwoIntDecimal.FromComponents(238418579, -7), TwoIntDecimal.FromComponents(119209290, -7),
        TwoIntDecimal.FromComponents(596046448, -8), TwoIntDecimal.FromComponents(298023224, -8),
        TwoIntDecimal.FromComponents(149011612, -8), TwoIntDecimal.FromComponents(745058060, -9),
        TwoIntDecimal.FromComponents(372529030, -9), TwoIntDecimal.FromComponents(186264515, -9),
        TwoIntDecimal.FromComponents(931322575, -10), TwoIntDecimal.FromComponents(465661287, -10),
    };

    private static readonly TwoIntDecimal[] CORDIC_SCALES = new TwoIntDecimal[]
    {
        TwoIntDecimal.FromComponents(100000000, 0), TwoIntDecimal.FromComponents(500000000, -1),
        TwoIntDecimal.FromComponents(250000000, -1), TwoIntDecimal.FromComponents(125000000, -1),
        TwoIntDecimal.FromComponents(625000000, -2), TwoIntDecimal.FromComponents(312500000, -2),
        TwoIntDecimal.FromComponents(156250000, -2), TwoIntDecimal.FromComponents(781250000, -3),
        TwoIntDecimal.FromComponents(390625000, -3), TwoIntDecimal.FromComponents(195312500, -3),
        TwoIntDecimal.FromComponents(976562500, -4), TwoIntDecimal.FromComponents(488281250, -4),
        TwoIntDecimal.FromComponents(244140625, -4), TwoIntDecimal.FromComponents(122070313, -4),
        TwoIntDecimal.FromComponents(610351563, -5), TwoIntDecimal.FromComponents(305175781, -5),
        TwoIntDecimal.FromComponents(152587891, -5), TwoIntDecimal.FromComponents(762939453, -6),
        TwoIntDecimal.FromComponents(381469727, -6), TwoIntDecimal.FromComponents(190734863, -6),
        TwoIntDecimal.FromComponents(953674316, -7), TwoIntDecimal.FromComponents(476837158, -7),
        TwoIntDecimal.FromComponents(238418579, -7), TwoIntDecimal.FromComponents(119209290, -7),
        TwoIntDecimal.FromComponents(596046448, -8), TwoIntDecimal.FromComponents(298023224, -8),
        TwoIntDecimal.FromComponents(149011612, -8), TwoIntDecimal.FromComponents(745058060, -9),
        TwoIntDecimal.FromComponents(372529030, -9), TwoIntDecimal.FromComponents(186264515, -9),
        TwoIntDecimal.FromComponents(931322575, -10), TwoIntDecimal.FromComponents(465661287, -10),
    };

    private static readonly TwoIntDecimal[] CORDIC_INVERSE_GAINS = new TwoIntDecimal[]
    {
        TwoIntDecimal.FromComponents(707106781, -1), TwoIntDecimal.FromComponents(632455532, -1),
        TwoIntDecimal.FromComponents(613571991, -1), TwoIntDecimal.FromComponents(608833913, -1),
        TwoIntDecimal.FromComponents(607648256, -1), TwoIntDecimal.FromComponents(607351770, -1),
        TwoIntDecimal.FromComponents(607277644, -1), TwoIntDecimal.FromComponents(607259112, -1),
        TwoIntDecimal.FromComponents(607254479, -1), TwoIntDecimal.FromComponents(607253321, -1),
        TwoIntDecimal.FromComponents(607253032, -1), TwoIntDecimal.FromComponents(607252959, -1),
        TwoIntDecimal.FromComponents(607252941, -1), TwoIntDecimal.FromComponents(607252937, -1),
        TwoIntDecimal.FromComponents(607252935, -1), TwoIntDecimal.FromComponents(607252935, -1),
        TwoIntDecimal.FromComponents(607252935, -1), TwoIntDecimal.FromComponents(607252935, -1),
        TwoIntDecimal.FromComponents(607252935, -1), TwoIntDecimal.FromComponents(607252935, -1),
        TwoIntDecimal.FromComponents(607252935, -1), TwoIntDecimal.FromComponents(607252935, -1),
        TwoIntDecimal.FromComponents(607252935, -1), TwoIntDecimal.FromComponents(607252935, -1),
        TwoIntDecimal.FromComponents(607252935, -1), TwoIntDecimal.FromComponents(607252935, -1),
        TwoIntDecimal.FromComponents(607252935, -1), TwoIntDecimal.FromComponents(607252935, -1),
        TwoIntDecimal.FromComponents(607252935, -1), TwoIntDecimal.FromComponents(607252935, -1),
        TwoIntDecimal.FromComponents(607252935, -1), TwoIntDecimal.FromComponents(607252935, -1),
    };


    // Internal methods.
    /* Roots. */
    internal static TwoIntDecimal Sqrt(TwoIntDecimal dec) => Sqrt(dec, DEFAULT_NEWTON_ITERATIONS);

    /* Newton's method on x^2 - dec, seeded from half the exponent. */
    internal static TwoIntDecimal Sqrt(TwoIntDecimal dec, int iterations)
    {
        if (TwoIntDecimal.IsNaN(dec) || (TwoIntDecimal.Sign(dec) < 0))
        {
            return TwoIntDecimal.NaN;
        }
        if (TwoIntDecimal.IsZero(dec) || TwoIntDecimal.IsInfinity(dec))
        {
            return dec;
        }

        int Steps = Clamp(iterations, MIN_NEWTON_ITERATIONS, MAX_NEWTON_ITERATIONS);
        int HalfExponent = FloorDivide(dec.Exponent, 2);
        int ExponentResidue = dec.Exponent - (HalfExponent * 2);
        TwoIntDecimal X = TwoIntDecimal.FromComponents(SQUARE_ROOT_SEEDS[ExponentResidue], HalfExponent);

        for (int i = 0; i < Steps; i++)
        {
            X = (X + (dec / X)) * TwoIntDecimal.Half;
        }
        return X;
    }

    internal static TwoIntDecimal Cbrt(TwoIntDecimal dec) => Cbrt(dec, DEFAULT_NEWTON_ITERATIONS);

    /* Newton's method on x^3 - dec, seeded from a third of the exponent. */
    internal static TwoIntDecimal Cbrt(TwoIntDecimal dec, int iterations)
    {
        if (TwoIntDecimal.IsNaN(dec))
        {
            return TwoIntDecimal.NaN;
        }
        if (TwoIntDecimal.IsZero(dec) || TwoIntDecimal.IsInfinity(dec))
        {
            return dec;
        }
        if (TwoIntDecimal.Sign(dec) < 0)
        {
            return -Cbrt(-dec, iterations);
        }

        int Steps = Clamp(iterations, MIN_NEWTON_ITERATIONS, MAX_NEWTON_ITERATIONS);
        int ThirdExponent = FloorDivide(dec.Exponent, 3);
        int ExponentResidue = dec.Exponent - (ThirdExponent * 3);
        TwoIntDecimal X = TwoIntDecimal.FromComponents(CUBE_ROOT_SEEDS[ExponentResidue], ThirdExponent);

        for (int i = 0; i < Steps; i++)
        {
            X = ((X + X) + (dec / (X * X))) / THREE;
        }
        return X;
    }

    internal static TwoIntDecimal NthRoot(TwoIntDecimal dec, int degree) =>
        NthRoot(dec, degree, DEFAULT_SERIES_ITERATIONS);

    /* The logarithm gives a seed good to most of the format's digits and two Newton steps on
     * x^degree - dec finish it, which is why the iteration count here drives the logarithm and the
     * power rather than a root iteration of its own. */
    internal static TwoIntDecimal NthRoot(TwoIntDecimal dec, int degree, int iterations)
    {
        if (TwoIntDecimal.IsNaN(dec) || (degree == 0))
        {
            return TwoIntDecimal.NaN;
        }
        if (degree == int.MinValue)
        {
            /* Its magnitude has no int to live in, and no value but one survives a root that deep. */
            return TwoIntDecimal.NaN;
        }
        if (degree < 0)
        {
            return TwoIntDecimal.One / NthRoot(dec, -degree, iterations);
        }
        if (degree == 1)
        {
            return dec;
        }

        bool IsDegreeEven = (degree % 2) == 0;
        if (TwoIntDecimal.Sign(dec) < 0)
        {
            if (IsDegreeEven)
            {
                return TwoIntDecimal.NaN;
            }
            return -NthRoot(-dec, degree, iterations);
        }
        if (TwoIntDecimal.IsZero(dec) || TwoIntDecimal.IsInfinity(dec))
        {
            return dec;
        }
        if (degree == 2)
        {
            return Sqrt(dec);
        }
        if (degree == 3)
        {
            return Cbrt(dec);
        }

        TwoIntDecimal X = Exp10(Log10(dec, iterations) / degree, iterations);
        if (!TwoIntDecimal.IsFinite(X) || TwoIntDecimal.IsZero(X))
        {
            return X;
        }

        const int POLISH_STEPS = 2;
        for (int i = 0; i < POLISH_STEPS; i++)
        {
            X = (((degree - 1) * X) + (dec / IntegerPow(X, degree - 1))) / degree;
        }
        return X;
    }

    /* Powers. */
    internal static TwoIntDecimal Pow(TwoIntDecimal dec, TwoIntDecimal power) =>
        Pow(dec, power, DEFAULT_SERIES_ITERATIONS);

    internal static TwoIntDecimal Pow(TwoIntDecimal dec, TwoIntDecimal power, int iterations)
    {
        /* These two come before the NaN checks on purpose: IEEE-754 gives anything raised to zero
         * and one raised to anything the value one, even when the other operand is a NaN. */
        if (TwoIntDecimal.IsZero(power))
        {
            return TwoIntDecimal.One;
        }
        if (dec == TwoIntDecimal.One)
        {
            return TwoIntDecimal.One;
        }
        if (TwoIntDecimal.IsNaN(dec) || TwoIntDecimal.IsNaN(power))
        {
            return TwoIntDecimal.NaN;
        }

        TwoIntDecimal Magnitude = TwoIntDecimal.Abs(dec);

        if (TwoIntDecimal.IsInfinity(power))
        {
            if (Magnitude == TwoIntDecimal.One)
            {
                return TwoIntDecimal.One;
            }
            bool IsGrowing = (Magnitude > TwoIntDecimal.One) == TwoIntDecimal.IsPositiveInfinity(power);
            return IsGrowing ? TwoIntDecimal.PositiveInfinity : TwoIntDecimal.Zero;
        }

        bool IsPowerAnInteger = TwoIntDecimal.IsInteger(power);
        bool IsPowerOdd = IsPowerAnInteger && !TwoIntDecimal.IsZero(power % TwoIntDecimal.Two);
        bool IsResultNegative = (TwoIntDecimal.Sign(dec) < 0) && IsPowerOdd;

        if (TwoIntDecimal.IsInfinity(dec))
        {
            if (TwoIntDecimal.Sign(power) > 0)
            {
                return IsResultNegative ? TwoIntDecimal.NegativeInfinity : TwoIntDecimal.PositiveInfinity;
            }
            return TwoIntDecimal.Zero;
        }
        if (TwoIntDecimal.IsZero(dec))
        {
            if (TwoIntDecimal.Sign(power) > 0)
            {
                return TwoIntDecimal.Zero;
            }
            return TwoIntDecimal.PositiveInfinity;
        }
        if ((TwoIntDecimal.Sign(dec) < 0) && !IsPowerAnInteger)
        {
            /* A negative base raised to a fractional power has no real value. */
            return TwoIntDecimal.NaN;
        }

        /* An integer power is worth squaring out: it keeps a negative base, and it rounds once per
         * bit of the exponent instead of once through a logarithm and back. */
        if (IsPowerAnInteger && (TwoIntDecimal.Abs(power) < TwoIntDecimal.FromComponents(100_000_000, 9)))
        {
            return IntegerPow(dec, (int)power);
        }

        TwoIntDecimal Result = Exp10(power * Log10(Magnitude, iterations), iterations);
        return IsResultNegative ? -Result : Result;
    }

    /* Exponentiation by squaring. Exact except for one rounding per multiplication, so about one
     * digit of the format's nine is lost to a power with thirty significant bits. */
    internal static TwoIntDecimal IntegerPow(TwoIntDecimal dec, int power)
    {
        if (power == 0)
        {
            return TwoIntDecimal.One;
        }
        if (power == int.MinValue)
        {
            /* Its magnitude does not fit an int, so it is split into a power that does and one more. */
            return TwoIntDecimal.One / (IntegerPow(dec, int.MaxValue) * dec);
        }

        bool IsPowerNegative = power < 0;
        int Remaining = IsPowerNegative ? -power : power;
        TwoIntDecimal Result = TwoIntDecimal.One;
        TwoIntDecimal Base = dec;

        while (Remaining > 0)
        {
            if ((Remaining % 2) == 1)
            {
                Result = Result * Base;
            }
            Remaining /= 2;
            if (Remaining > 0)
            {
                Base = Base * Base;
            }
        }

        return IsPowerNegative ? (TwoIntDecimal.One / Result) : Result;
    }

    internal static TwoIntDecimal Exp(TwoIntDecimal dec) => Exp(dec, DEFAULT_SERIES_ITERATIONS);

    internal static TwoIntDecimal Exp(TwoIntDecimal dec, int iterations) => Exp10(dec * LOG10_E, iterations);

    internal static TwoIntDecimal Exp10(TwoIntDecimal power) => Exp10(power, DEFAULT_SERIES_ITERATIONS);

    /* Ten raised to an arbitrary power. The whole part of the exponent is free, because it is a
     * shift of this format's own exponent, so only the fractional part has to be computed and it is
     * always in [0, 1) where the series for it converges quickly. */
    internal static TwoIntDecimal Exp10(TwoIntDecimal power, int iterations)
    {
        if (TwoIntDecimal.IsNaN(power))
        {
            return TwoIntDecimal.NaN;
        }
        if (TwoIntDecimal.IsPositiveInfinity(power))
        {
            return TwoIntDecimal.PositiveInfinity;
        }
        if (TwoIntDecimal.IsNegativeInfinity(power))
        {
            return TwoIntDecimal.Zero;
        }

        TwoIntDecimal WholePart = TwoIntDecimal.Floor(power);
        TwoIntDecimal FractionPart = power - WholePart;

        /* The conversion saturates and the shift below saturates with it, so an exponent far
         * outside the format's range still lands on an infinity or on zero. */
        return TwoIntDecimal.ScaleByPowerOfTen(ExpSeries(FractionPart * LN_10, iterations), (int)WholePart);
    }

    /* Logarithms. */
    internal static TwoIntDecimal Log10(TwoIntDecimal dec) => Log10(dec, DEFAULT_SERIES_ITERATIONS);

    /* Base ten logarithm. This format stores a base ten exponent, so the whole part of the answer
     * is read straight off the value and only the mantissa needs a series. */
    internal static TwoIntDecimal Log10(TwoIntDecimal dec, int iterations)
    {
        if (TwoIntDecimal.IsNaN(dec) || (TwoIntDecimal.Sign(dec) < 0))
        {
            return TwoIntDecimal.NaN;
        }
        if (TwoIntDecimal.IsZero(dec))
        {
            return TwoIntDecimal.NegativeInfinity;
        }
        if (TwoIntDecimal.IsInfinity(dec))
        {
            return TwoIntDecimal.PositiveInfinity;
        }

        int WholePart = dec.Exponent;
        TwoIntDecimal Mantissa = TwoIntDecimal.FromComponents(dec.Mantissa, 0);

        /* Centring the mantissa on one keeps the series argument below 0.52 instead of 0.82, which
         * is worth several terms. */
        if (Mantissa >= SQRT_10)
        {
            Mantissa = Mantissa / TwoIntDecimal.Ten;
            WholePart++;
        }

        return WholePart + (NaturalLogSeries(Mantissa, iterations) * LOG10_E);
    }

    internal static TwoIntDecimal Log(TwoIntDecimal dec) => Log(dec, DEFAULT_SERIES_ITERATIONS);

    internal static TwoIntDecimal Log(TwoIntDecimal dec, int iterations) => Log10(dec, iterations) * LN_10;

    internal static TwoIntDecimal Log2(TwoIntDecimal dec) => Log2(dec, DEFAULT_SERIES_ITERATIONS);

    internal static TwoIntDecimal Log2(TwoIntDecimal dec, int iterations) => Log10(dec, iterations) / LOG10_2;

    internal static TwoIntDecimal Log(TwoIntDecimal value, TwoIntDecimal numberBase) =>
        Log(value, numberBase, DEFAULT_SERIES_ITERATIONS);

    internal static TwoIntDecimal Log(TwoIntDecimal value, TwoIntDecimal numberBase, int iterations)
    {
        return Log10(value, iterations) / Log10(numberBase, iterations);
    }

    /* Angles. */
    internal static TwoIntDecimal DegToRad(TwoIntDecimal deg) => deg * DEGREES_TO_RADIANS;

    internal static TwoIntDecimal RadToDeg(TwoIntDecimal rad) => rad * RADIANS_TO_DEGREES;

    /* Trigonometry. */
    internal static TwoIntDecimal Sin(TwoIntDecimal radians) => Sin(radians, DEFAULT_CORDIC_ITERATIONS);

    internal static TwoIntDecimal Sin(TwoIntDecimal radians, int iterations)
    {
        SinCos(radians, iterations, out TwoIntDecimal Sine, out _);
        return Sine;
    }

    internal static TwoIntDecimal Cos(TwoIntDecimal radians) => Cos(radians, DEFAULT_CORDIC_ITERATIONS);

    internal static TwoIntDecimal Cos(TwoIntDecimal radians, int iterations)
    {
        SinCos(radians, iterations, out _, out TwoIntDecimal Cosine);
        return Cosine;
    }

    internal static TwoIntDecimal Tan(TwoIntDecimal radians) => Tan(radians, DEFAULT_CORDIC_ITERATIONS);

    internal static TwoIntDecimal Tan(TwoIntDecimal radians, int iterations)
    {
        SinCos(radians, iterations, out TwoIntDecimal Sine, out TwoIntDecimal Cosine);
        return Sine / Cosine;
    }

    internal static void SinCos(TwoIntDecimal radians, out TwoIntDecimal sine, out TwoIntDecimal cosine) =>
        SinCos(radians, DEFAULT_CORDIC_ITERATIONS, out sine, out cosine);

    /* Both at once, because one CORDIC rotation produces both and the callers above would otherwise
     * pay for the same rotation twice. */
    internal static void SinCos(TwoIntDecimal radians, int iterations, out TwoIntDecimal sine,
        out TwoIntDecimal cosine)
    {
        if (!TwoIntDecimal.IsFinite(radians) || (TwoIntDecimal.Abs(radians) >= TRIGONOMETRY_ARGUMENT_LIMIT))
        {
            sine = TwoIntDecimal.NaN;
            cosine = TwoIntDecimal.NaN;
            return;
        }

        if (TwoIntDecimal.IsZero(radians))
        {
            /* A rotation by nothing still leaves CORDIC's residue behind, and the answer is known. */
            sine = TwoIntDecimal.Zero;
            cosine = TwoIntDecimal.One;
            return;
        }

        /* Reduce onto a quarter turn, which is well inside the rotation's convergence range, and
         * remember which quarter it came from. */
        TwoIntDecimal QuarterTurns = TwoIntDecimal.Round(radians / HALF_PI);
        TwoIntDecimal Remainder = radians - (QuarterTurns * HALF_PI);

        RotateCircular(Remainder, iterations, out TwoIntDecimal Cosine, out TwoIntDecimal Sine);

        /* A scoreboard remainder floors where C# truncates, so the negative case is lifted by hand
         * and the two agree. */
        int Quarter = (int)QuarterTurns % 4;
        if (Quarter < 0)
        {
            Quarter += 4;
        }

        switch (Quarter)
        {
            case 0:
                sine = Sine;
                cosine = Cosine;
                break;
            case 1:
                sine = Cosine;
                cosine = -Sine;
                break;
            case 2:
                sine = -Sine;
                cosine = -Cosine;
                break;
            default:
                sine = -Cosine;
                cosine = Sine;
                break;
        }
    }

    internal static TwoIntDecimal Asin(TwoIntDecimal dec) => Asin(dec, DEFAULT_CORDIC_ITERATIONS);

    internal static TwoIntDecimal Asin(TwoIntDecimal dec, int iterations)
    {
        if (TwoIntDecimal.IsNaN(dec) || (TwoIntDecimal.Abs(dec) > TwoIntDecimal.One))
        {
            return TwoIntDecimal.NaN;
        }
        /* Splitting 1 - x^2 into its two factors keeps the subtraction exact right up to the ends
         * of the domain, where forming the square first would have thrown away most of the digits. */
        return Atan2(dec, Sqrt(TwoIntDecimal.One - dec) * Sqrt(TwoIntDecimal.One + dec), iterations);
    }

    internal static TwoIntDecimal Acos(TwoIntDecimal dec) => Acos(dec, DEFAULT_CORDIC_ITERATIONS);

    internal static TwoIntDecimal Acos(TwoIntDecimal dec, int iterations)
    {
        if (TwoIntDecimal.IsNaN(dec) || (TwoIntDecimal.Abs(dec) > TwoIntDecimal.One))
        {
            return TwoIntDecimal.NaN;
        }
        /* The half angle form, for the same reason as in Asin. */
        return TwoIntDecimal.Two * Atan2(Sqrt(TwoIntDecimal.One - dec), Sqrt(TwoIntDecimal.One + dec),
            iterations);
    }

    internal static TwoIntDecimal Atan(TwoIntDecimal dec) => Atan(dec, DEFAULT_CORDIC_ITERATIONS);

    internal static TwoIntDecimal Atan(TwoIntDecimal dec, int iterations) =>
        Atan2(dec, TwoIntDecimal.One, iterations);

    internal static TwoIntDecimal Atan2(TwoIntDecimal y, TwoIntDecimal x) =>
        Atan2(y, x, DEFAULT_CORDIC_ITERATIONS);

    internal static TwoIntDecimal Atan2(TwoIntDecimal y, TwoIntDecimal x, int iterations)
    {
        if (TwoIntDecimal.IsNaN(y) || TwoIntDecimal.IsNaN(x))
        {
            return TwoIntDecimal.NaN;
        }

        if (TwoIntDecimal.IsInfinity(y) || TwoIntDecimal.IsInfinity(x))
        {
            return GetInfiniteAtan2(y, x);
        }
        if (TwoIntDecimal.IsZero(y))
        {
            /* Without a negative zero the half turn below can only be reached from a negative x. */
            return TwoIntDecimal.Sign(x) < 0 ? TwoIntDecimal.Pi : TwoIntDecimal.Zero;
        }
        if (TwoIntDecimal.IsZero(x))
        {
            return TwoIntDecimal.Sign(y) < 0 ? -HALF_PI : HALF_PI;
        }

        if (TwoIntDecimal.Sign(x) > 0)
        {
            return VectorCircular(x, y, iterations);
        }

        TwoIntDecimal HalfTurn = TwoIntDecimal.Sign(y) < 0 ? -TwoIntDecimal.Pi : TwoIntDecimal.Pi;
        return HalfTurn - VectorCircular(-x, y, iterations);
    }

    /* Hyperbolic functions. */
    internal static TwoIntDecimal Sinh(TwoIntDecimal dec) => Sinh(dec, DEFAULT_SERIES_ITERATIONS);

    internal static TwoIntDecimal Sinh(TwoIntDecimal dec, int iterations)
    {
        if (!TwoIntDecimal.IsFinite(dec) || TwoIntDecimal.IsZero(dec))
        {
            return dec;
        }

        /* Near zero the two exponentials are almost equal and subtracting them would throw away
         * most of the digits, so the series is used instead. */
        if (TwoIntDecimal.Abs(dec) < TwoIntDecimal.Half)
        {
            return SinhSeries(dec, iterations);
        }

        TwoIntDecimal Raised = Exp(dec, iterations);
        return (Raised - (TwoIntDecimal.One / Raised)) * TwoIntDecimal.Half;
    }

    internal static TwoIntDecimal Cosh(TwoIntDecimal dec) => Cosh(dec, DEFAULT_SERIES_ITERATIONS);

    internal static TwoIntDecimal Cosh(TwoIntDecimal dec, int iterations)
    {
        if (TwoIntDecimal.IsNaN(dec))
        {
            return TwoIntDecimal.NaN;
        }
        if (TwoIntDecimal.IsInfinity(dec))
        {
            return TwoIntDecimal.PositiveInfinity;
        }

        TwoIntDecimal Raised = Exp(dec, iterations);
        if (TwoIntDecimal.IsInfinity(Raised))
        {
            return TwoIntDecimal.PositiveInfinity;
        }
        return (Raised + (TwoIntDecimal.One / Raised)) * TwoIntDecimal.Half;
    }

    internal static TwoIntDecimal Tanh(TwoIntDecimal dec) => Tanh(dec, DEFAULT_SERIES_ITERATIONS);

    internal static TwoIntDecimal Tanh(TwoIntDecimal dec, int iterations)
    {
        if (TwoIntDecimal.IsNaN(dec))
        {
            return TwoIntDecimal.NaN;
        }
        if (TwoIntDecimal.IsZero(dec))
        {
            return dec;
        }

        /* Far from zero both halves overflow while their ratio has long since settled on one. */
        if (TwoIntDecimal.Abs(dec) >= TANH_SATURATION_POINT)
        {
            return TwoIntDecimal.Sign(dec) < 0 ? -TwoIntDecimal.One : TwoIntDecimal.One;
        }

        return Sinh(dec, iterations) / Cosh(dec, iterations);
    }


    // Private methods.
    /* Primitives, matching the ones in TwoIntDecimal: floor division and a clamp, on non-negative
     * divisors only, so that C# and a scoreboard agree. */
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

    private static int FloorDivide(int value, int divisor)
    {
        if (value >= 0)
        {
            return value / divisor;
        }
        return -((divisor - 1 - value) / divisor);
    }

    /* Series. */
    /* e^x for x in [0, ln 10). Every term is positive, so nothing cancels and the error is just the
     * tail that was not summed. */
    private static TwoIntDecimal ExpSeries(TwoIntDecimal dec, int iterations)
    {
        int Steps = Clamp(iterations, MIN_SERIES_ITERATIONS, MAX_SERIES_ITERATIONS);
        TwoIntDecimal Term = TwoIntDecimal.One;
        TwoIntDecimal Sum = TwoIntDecimal.One;

        for (int i = 1; i <= Steps; i++)
        {
            Term = (Term * dec) / i;
            Sum = Sum + Term;
        }
        return Sum;
    }

    /* ln(m) as 2 * atanh((m - 1) / (m + 1)), which converges on the whole interval the caller
     * reduces its mantissa to and needs no table. */
    private static TwoIntDecimal NaturalLogSeries(TwoIntDecimal dec, int iterations)
    {
        int Steps = Clamp(iterations, MIN_SERIES_ITERATIONS, MAX_SERIES_ITERATIONS);
        TwoIntDecimal Ratio = (dec - TwoIntDecimal.One) / (dec + TwoIntDecimal.One);
        TwoIntDecimal RatioSquared = Ratio * Ratio;
        TwoIntDecimal Term = Ratio;
        TwoIntDecimal Sum = Ratio;

        for (int i = 1; i <= Steps; i++)
        {
            Term = Term * RatioSquared;
            Sum = Sum + (Term / ((2 * i) + 1));
        }
        return Sum + Sum;
    }

    /* sinh(x) for small x, where the difference of two exponentials would cancel. */
    private static TwoIntDecimal SinhSeries(TwoIntDecimal dec, int iterations)
    {
        int Steps = Clamp(iterations, MIN_SERIES_ITERATIONS, MAX_SERIES_ITERATIONS);
        TwoIntDecimal Squared = dec * dec;
        TwoIntDecimal Term = dec;
        TwoIntDecimal Sum = dec;

        for (int i = 1; i <= Steps; i++)
        {
            Term = (Term * Squared) / ((2 * i) * ((2 * i) + 1));
            Sum = Sum + Term;
        }
        return Sum;
    }

    /* CORDIC. */
    /* Circular CORDIC in rotation mode: turns the unit vector by the angle and reads the cosine and
     * sine off it. The angle must already sit inside the convergence range, which is the sum of the
     * table's angles and is wider than the quarter turn the callers reduce to. Seeding x with the
     * reciprocal of the rotation's gain saves a division at the end. */
    private static void RotateCircular(TwoIntDecimal angle, int iterations, out TwoIntDecimal cosine,
        out TwoIntDecimal sine)
    {
        int Steps = Clamp(iterations, MIN_CORDIC_ITERATIONS, MAX_CORDIC_ITERATIONS);
        TwoIntDecimal X = CORDIC_INVERSE_GAINS[Steps - 1];
        TwoIntDecimal Y = TwoIntDecimal.Zero;
        TwoIntDecimal Z = angle;

        for (int i = 0; i < Steps; i++)
        {
            TwoIntDecimal ScaledX = X * CORDIC_SCALES[i];
            TwoIntDecimal ScaledY = Y * CORDIC_SCALES[i];

            if (TwoIntDecimal.Sign(Z) >= 0)
            {
                X = X - ScaledY;
                Y = Y + ScaledX;
                Z = Z - CORDIC_ANGLES[i];
            }
            else
            {
                X = X + ScaledY;
                Y = Y - ScaledX;
                Z = Z + CORDIC_ANGLES[i];
            }
        }

        cosine = X;
        sine = Y;
    }

    /* Circular CORDIC in vectoring mode: turns the vector onto the x axis and reads the angle it
     * travelled, which is atan(y / x). Only the ratio matters, so no scaling is needed, but x has to
     * be positive -- Atan2 folds the other half turns in itself. */
    private static TwoIntDecimal VectorCircular(TwoIntDecimal x, TwoIntDecimal y, int iterations)
    {
        int Steps = Clamp(iterations, MIN_CORDIC_ITERATIONS, MAX_CORDIC_ITERATIONS);
        TwoIntDecimal X = x;
        TwoIntDecimal Y = y;
        TwoIntDecimal Z = TwoIntDecimal.Zero;

        for (int i = 0; i < Steps; i++)
        {
            TwoIntDecimal ScaledX = X * CORDIC_SCALES[i];
            TwoIntDecimal ScaledY = Y * CORDIC_SCALES[i];

            if (TwoIntDecimal.Sign(Y) < 0)
            {
                X = X - ScaledY;
                Y = Y + ScaledX;
                Z = Z - CORDIC_ANGLES[i];
            }
            else
            {
                X = X + ScaledY;
                Y = Y - ScaledX;
                Z = Z + CORDIC_ANGLES[i];
            }
        }

        return Z;
    }

    /* The eight directions an infinite argument can point in. */
    private static TwoIntDecimal GetInfiniteAtan2(TwoIntDecimal y, TwoIntDecimal x)
    {
        TwoIntDecimal QuarterPi = HALF_PI * TwoIntDecimal.Half;

        if (TwoIntDecimal.IsInfinity(y) && TwoIntDecimal.IsInfinity(x))
        {
            TwoIntDecimal Angle = TwoIntDecimal.Sign(x) > 0 ? QuarterPi : (QuarterPi * THREE);
            return TwoIntDecimal.Sign(y) < 0 ? -Angle : Angle;
        }
        if (TwoIntDecimal.IsInfinity(y))
        {
            return TwoIntDecimal.Sign(y) < 0 ? -HALF_PI : HALF_PI;
        }
        if (TwoIntDecimal.Sign(x) > 0)
        {
            return TwoIntDecimal.Zero;
        }
        return TwoIntDecimal.Sign(y) < 0 ? -TwoIntDecimal.Pi : TwoIntDecimal.Pi;
    }
}
