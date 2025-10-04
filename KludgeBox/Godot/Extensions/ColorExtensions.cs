using Godot;

namespace KludgeBox.Godot.Extensions;

public static class ColorExtensions
{
    /// <summary>
    /// Converts a grayscale value to a Color object.
    /// </summary>
    /// <param name="value">The grayscale value to be used for all color components.</param>
    /// <returns>A Color object representing the grayscale value.</returns>
    /// <remarks>
    /// The grayscale value will be applied to all color components (R, G, and B) of the resulting Color object.
    /// </remarks>
    public static Color Col(float value) => new Color(value, value, value);

    /// <summary>
    /// Extension method that converts a color to grayscale.
    /// </summary>
    /// <param name="color">The color to convert.</param>
    /// <returns>A grayscale color.</returns>
    public static Color Grayscale(this Color color)
    {
        float gray = color.GrayscaleValue();
        return new Color(gray, gray, gray, color.A);
    }

    /// <summary>
    /// Calculates the grayscale value of a color.
    /// </summary>
    /// <param name="color">The color to calculate the grayscale value for.</param>
    /// <returns>The grayscale value of the color.</returns>
    public static float GrayscaleValue(this Color color)
    {
        return 0.299f * color.R + 0.587f * color.G + 0.114f * color.B;
    }

    /// <summary>
    /// Creates a new color with RGB values set to 0.
    /// </summary>
    /// <returns>A color with RGB values set to 0.</returns>
    public static Color Col() => new Color(0, 0, 0);

    /// <summary>
    /// Creates a new color with the specified RGB and alpha values.
    /// </summary>
    /// <param name="r">The red component of the color.</param>
    /// <param name="g">The green component of the color.</param>
    /// <param name="b">The blue component of the color.</param>
    /// <param name="a">The alpha component of the color (default is 1).</param>
    /// <returns>A new color with the specified RGB and alpha values.</returns>
    public static Color Col(float r, float g, float b, float a = 1) => new Color(r, g, b, a);
}
