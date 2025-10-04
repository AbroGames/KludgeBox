using Godot;

namespace KludgeBox.Godot.Extensions;

public static class Node2DExtensions
{
    
    /// <summary>
    /// Gets the upward vector relative to the given Node2D's rotation.
    /// </summary>
    public static Vector2 Up(this Node2D node) => Vector2.Up.Rotated(node.Rotation);

    /// <summary>
    /// Gets the downward vector relative to the given Node2D's rotation.
    /// </summary>
    public static Vector2 Down(this Node2D node) => Vector2.Down.Rotated(node.Rotation);

    /// <summary>
    /// Gets the leftward vector relative to the given Node2D's rotation.
    /// </summary>
    public static Vector2 Left(this Node2D node) => Vector2.Left.Rotated(node.Rotation);

    /// <summary>
    /// Gets the rightward vector relative to the given Node2D's rotation.
    /// </summary>
    public static Vector2 Right(this Node2D node) => Vector2.Right.Rotated(node.Rotation);

    /// <summary>
    /// Gets the upward vector relative to the global rotation of the given Node2D.
    /// </summary>
    public static Vector2 GlobalUp(this Node2D node) => Vector2.Up.Rotated(node.GlobalRotation);

    /// <summary>
    /// Gets the downward vector relative to the global rotation of the given Node2D.
    /// </summary>
    public static Vector2 GlobalDown(this Node2D node) => Vector2.Down.Rotated(node.GlobalRotation);

    /// <summary>
    /// Gets the leftward vector relative to the global rotation of the given Node2D.
    /// </summary>
    public static Vector2 GlobalLeft(this Node2D node) => Vector2.Left.Rotated(node.GlobalRotation);

    /// <summary>
    /// Gets the rightward vector relative to the global rotation of the given Node2D.
    /// </summary>
    public static Vector2 GlobalRight(this Node2D node) => Vector2.Right.Rotated(node.GlobalRotation);

    /// <summary>
    /// Gets the direction vector from the 'from' Node2D to the 'to' Node2D.
    /// </summary>
    public static Vector2 DirectionFrom(this Node2D to, Node2D from) => from.Position.DirectionTo(to.Position);

    /// <summary>
    /// Gets the direction vector from the 'from' Node2D to the 'to' Node2D.
    /// </summary>
    public static Vector2 DirectionTo(this Node2D from, Node2D to) => from.Position.DirectionTo(to.Position);

    /// <summary>
    /// Gets the global direction vector from the 'from' Node2D to the 'to' Node2D.
    /// </summary>
    public static Vector2 GlobalDirectionFrom(this Node2D to, Node2D from) => from.GlobalPosition.DirectionTo(to.GlobalPosition);

    /// <summary>
    /// Gets the global direction vector from the 'from' Node2D to the 'to' Node2D.
    /// </summary>
    public static Vector2 GlobalDirectionTo(this Node2D from, Node2D to) => from.GlobalPosition.DirectionTo(to.GlobalPosition);

    
    /// <summary>
    /// Calculates the distance between two Node2D objects.
    /// </summary>
    public static double DistanceTo(this Node2D node, Node2D other) => node.Position.DistanceTo(other.Position);

    /// <summary>
    /// Calculates the distance between a Node2D object and a Vector2 position.
    /// </summary>
    public static double DistanceTo(this Node2D node, Vector2 pos) => node.Position.DistanceTo(pos);

    /// <summary>
    /// Calculates the distance between a Vector2 position and a Node2D object.
    /// </summary>
    public static double DistanceTo(this Vector2 pos, Node2D other) => pos.DistanceTo(other.Position);

    
    /// <summary>
    /// Calculates the global distance between the calling Node2D instance and another Node2D.
    /// </summary>
    public static double GlobalDistanceTo(this Node2D node, Node2D other) => node.GlobalPosition.DistanceTo(other.GlobalPosition);

    /// <summary>
    /// Calculates the global distance between the calling Node2D instance and a specified position.
    /// </summary>
    public static double GlobalDistanceTo(this Node2D node, Vector2 pos) => node.GlobalPosition.DistanceTo(pos);

    /// <summary>
    /// Calculates the global distance between a specified position and the Node2D instance.
    /// </summary>
    public static double GlobalDistanceTo(this Vector2 pos, Node2D other) => pos.DistanceTo(other.GlobalPosition);
}
