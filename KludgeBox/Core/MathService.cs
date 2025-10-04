using System.Runtime.CompilerServices;
using Godot;

namespace KludgeBox.Core;

public class MathService
{
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public float Clamp(float value, float min, float max) => Mathf.Max(Mathf.Min(value, Mathf.Max(max, min)), Mathf.Min(min, max));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public double Clamp(double value, double min, double max) => System.Math.Clamp(value, System.Math.Min(min, max), System.Math.Max(min, max));
    
    /// <summary>
    /// Determines whether the specified value is a power of two.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the value is a power of two; otherwise, <c>false</c>.</returns>
    public bool IsPowerOfTwo(int value) => value != 0 && (value & value - 1) == 0;

    /// <summary>
    /// Returns the next power of two that is greater than or equal to the specified value.
    /// </summary>
    /// <param name="value">The value to find the next power of two for.</param>
    /// <returns>The next power of two that is greater than or equal to the specified value.</returns>
    public int NextPowerOfTwo(int value)
    {
        if (value == 0) return 1;
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    /// <summary>
    /// Determines whether the specified value is a power of two.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the value is a power of two; otherwise, <c>false</c>.</returns>
    public bool IsPowerOfTwo(long value) => value != 0 && (value & (value - 1)) == 0;

    /// <summary>
    /// Returns the next power of two that is greater than or equal to the specified value.
    /// </summary>
    /// <param name="value">The value to find the next power of two for.</param>
    /// <returns>The next power of two that is greater than or equal to the specified value.</returns>
    public long NextPowerOfTwo(long value)
    {
        if (value == 0) return 1;
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        value |= value >> 32;
        return value + 1;
    }
    
    /// <summary>
    /// Maps a value from one range to another range.
    /// </summary>
    /// <param name="value">The value to map.</param>
    /// <param name="froma">The start of the original range.</param>
    /// <param name="toa">The end of the original range.</param>
    /// <param name="fromb">The start of the target range.</param>
    /// <param name="tob">The end of the target range.</param>
    /// <returns>The mapped value.</returns>
    public float Map(float value, float froma, float toa, float fromb, float tob)
    {
        return fromb + (value - froma) * (tob - fromb) / (toa - froma);
    }

    /// <summary>
    /// Maps a value from the default range [0, 1] to a target range.
    /// </summary>
    /// <param name="value">The value to map.</param>
    /// <param name="from">The start of the target range.</param>
    /// <param name="to">The end of the target range.</param>
    /// <returns>The mapped value.</returns>
    public float Map(float value, float from, float to)
    {
        return Map(value, 0, 1, from, to);
    }
    
    /// <summary>
    /// Calculates the number of digits in an integer.
    /// </summary>
    /// <param name="n">The integer value.</param>
    /// <returns>The number of digits in the integer.</returns>
    public int Digits(int n)
    {
        return n < 100000
            ? n < 100
                ? n < 10 ? 1 : 2
                : n < 1000 ? 3
                    : n < 10000 ? 4
                        : 5
            : n < 10000000
                ? n < 1000000 ? 6 : 7
                : n < 100000000 ? 8
                    : n < 1000000000 ? 9 : 10;
    }

    /// <summary>
    /// Calculates the number of digits in a long integer.
    /// </summary>
    /// <param name="n">The long integer value.</param>
    /// <returns>The number of digits in the long integer.</returns>
    public int Digits(long n)
    {
        return n == 0 ? 1 : (int)(System.Math.Log10((double)n) + 1);
    }
}
