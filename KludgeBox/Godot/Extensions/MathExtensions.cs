using Godot;

namespace KludgeBox.Godot.Extensions;

public static class MathExtensions
{
    
    // Epsilons
    public const float EpsilonFloat = 1E-06f;
    public const double EpsilonDouble = 1E-14;
    
    /// <summary>
    /// Checks if two single-precision floating-point numbers are approximately equal.
    /// </summary>
    /// <param name="a">The first floating-point number.</param>
    /// <param name="b">The second floating-point number.</param>
    /// <returns>
    /// True if the numbers are approximately equal within a small tolerance, false otherwise.
    /// </returns>
    public static bool IsEqualApprox(this float a, float b)
    {
        if (a == b)
        {
            return true;
        }

        float tolerance = EpsilonFloat * Mathf.Abs(a);
        if (tolerance < EpsilonFloat)
        {
            tolerance = EpsilonFloat;
        }

        return Mathf.Abs(a - b) < tolerance;
    }

    /// <summary>
    /// Checks if two double-precision floating-point numbers are approximately equal.
    /// </summary>
    /// <param name="a">The first floating-point number.</param>
    /// <param name="b">The second floating-point number.</param>
    /// <returns>
    /// True if the numbers are approximately equal within a small tolerance, false otherwise.
    /// </returns>
    public static bool IsEqualApprox(this double a, double b)
    {
        if (a == b)
        {
            return true;
        }

        double tolerance = EpsilonDouble * Mathf.Abs(a);
        if (tolerance < EpsilonDouble)
        {
            tolerance = EpsilonDouble;
        }

        return Mathf.Abs(a - b) < tolerance;
    }
    
    /// <summary>
    /// Gets the Unit in the Last Place (ULP) of the specified double-precision floating-point number.
    /// </summary>
    /// <param name="value">The double-precision floating-point number.</param>
    /// <returns>The ULP of the specified double-precision floating-point number.</returns>
    /// <remarks>
    /// The Unit in the Last Place (ULP) is the difference between the smallest representable
    /// floating-point value greater than the given value and the value itself. It is a measure
    /// of the precision or the gap between consecutive floating-point numbers.
    /// </remarks>
    public static double GetUlp(this double value)
    {
        long bits = BitConverter.DoubleToInt64Bits(value);
        double nextValue = BitConverter.Int64BitsToDouble(bits + 1);
        double result = nextValue - value;
        return result;
    }

    /// <summary>
    /// Gets the Unit in the Last Place (ULP) of the specified single-precision floating-point number.
    /// </summary>
    /// <param name="value">The single-precision floating-point number.</param>
    /// <returns>The ULP of the specified single-precision floating-point number.</returns>
    /// <remarks>
    /// The Unit in the Last Place (ULP) is the difference between the smallest representable
    /// floating-point value greater than the given value and the value itself. It is a measure
    /// of the precision or the gap between consecutive floating-point numbers.
    /// </remarks>
    public static float GetUlp(this float value)
    {
        int bits = BitConverter.SingleToInt32Bits(value);
        float nextValue = BitConverter.Int32BitsToSingle(bits + 1);
        float result = nextValue - value;
        return result;
    }
}