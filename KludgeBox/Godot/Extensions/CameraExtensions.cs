using Godot;

namespace KludgeBox.Godot.Extensions;

public static class CameraExtensions
{
    /// <summary>
    /// Extension method that returns the visible rectangle (in global coordinates) of the Camera2D node's viewport.
    /// The visible rectangle is determined based on the camera's position, zoom level, and viewport size.
    /// </summary>
    /// <param name="camera">The Camera2D node for which to calculate the visible rectangle.</param>
    /// <returns>The visible rectangle in global coordinates.</returns>
    public static Rect2 GetVisibleRect(this Camera2D camera)
    {
        var viewport = camera.GetViewportRect();
        var cameraSize = viewport.Size / camera.Zoom;
        return new Rect2
        {
            Position = camera.Position - cameraSize / 2,
            Size = cameraSize
        };
    }

    /// <summary>
    /// Extension method that calculates and returns the radius of the visible area of the Camera2D node's viewport.
    /// The radius is determined based on the visible rectangle's circumradius.
    /// </summary>
    /// <param name="camera">The Camera2D node for which to calculate the visible area radius.</param>
    /// <returns>The radius of the visible area in global coordinates.</returns>
    public static double GetRadius(this Camera2D camera)
    {
        return camera.GetVisibleRect().GetCircumradius();
    }
}
