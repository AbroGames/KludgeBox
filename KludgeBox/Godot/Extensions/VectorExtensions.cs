using Godot;

namespace KludgeBox.Godot.Extensions;

public static class VectorExtensions
{
    
    public static Vector2 Vec2() => Vector2.Zero;
    public static Vector2 Vec2(float x, float y) => new(x, y);
    public static Vector2 Vec2(float xy) => new(xy, xy);
    public static Vector2 Vec2(double x, double y) => new((float)x, (float)y);
    public static Vector2 Vec2(double xy) => new((float)xy, (float)xy);
    public static Vector2 Vec2(Vector2I vec) => new(vec.X, vec.Y);
    public static Vector2 AsVec2(this Vector2I vec) => Vec2(vec);
    
    public static Vector2I Vec2I() => Vector2I.Zero;
    public static Vector2I Vec2I(int x, int y) => new(x, y);
    public static Vector2I Vec2I(int xy) => new(xy, xy);
    public static Vector2I Vec2I(float x, float y) => new((int)x, (int)y);
    public static Vector2I Vec2I(float xy) => new((int)xy, (int)xy);
    public static Vector2I Vec2I(double x, double y) => new((int)x, (int)y);
    public static Vector2I Vec2I(double xy) => new((int)xy, (int)xy);
    public static Vector2I Vec2I(Vector2 vec) => new((int)vec.X, (int)vec.Y);
    public static Vector2I AsVec2I(this Vector2 vec) => Vec2I(vec);
}