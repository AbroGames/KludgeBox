using Godot;

namespace KludgeBox.Godot.Extensions;

public static class RectExtensions
{
    public static Vector2[] ToPolygon(this Rect2 rect)
    {
        return [
            rect.Position, // top left
            rect.Position + Vec2(rect.Size.X, 0), // top right
            rect.End, // bottom right
            rect.End - Vec2(rect.Size.X, 0) // bottom left
        ];
    }
    
    
    public static Vector2I[] ToPolygon(this Rect2I rect)
    {
        return [
            rect.Position, // top left
            rect.Position + Vec2I(rect.Size.X, 0), // top right
            rect.End, // bottom right
            rect.End - Vec2I(rect.Size.X, 0) // bottom left
        ];
    }
    
    /// <summary>
    /// Gets the X-coordinate of the top-left corner of the specified Rect2.
    /// </summary>
    /// <param name="rect">The Rect2 object.</param>
    /// <returns>The X-coordinate of the top-left corner.</returns>
    public static float X(this Rect2 rect) => rect.Position.X;

    /// <summary>
    /// Gets the Y-coordinate of the top-left corner of the specified Rect2.
    /// </summary>
    /// <param name="rect">The Rect2 object.</param>
    /// <returns>The Y-coordinate of the top-left corner.</returns>
    public static float Y(this Rect2 rect) => rect.Position.Y;

    /// <summary>
    /// Gets the width of the specified Rect2.
    /// </summary>
    /// <param name="rect">The Rect2 object.</param>
    /// <returns>The width of the Rect2.</returns>
    public static float Width(this Rect2 rect) => rect.Size.X;

    /// <summary>
    /// Gets the height of the specified Rect2.
    /// </summary>
    /// <param name="rect">The Rect2 object.</param>
    /// <returns>The height of the Rect2.</returns>
    public static float Height(this Rect2 rect) => rect.Size.Y;

    public static float GetCircumradius(this Rect2 rect)
    {
        return Mathf.Sqrt(rect.Width() * rect.Width() + rect.Height() * rect.Height()) / 2;
    }
}
