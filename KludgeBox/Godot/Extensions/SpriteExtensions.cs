using Godot;

namespace KludgeBox.Godot.Extensions;

public static class SpriteExtensions
{
    /// <summary>
    /// Gets the absolute size of the sprite, taking into account the texture and global scale.
    /// </summary>
    /// <param name="sprite">The Sprite2D object.</param>
    /// <returns>The absolute size of the sprite.</returns>
    public static Vector2 GetAbsoluteSize(this Sprite2D sprite)
    {
        var texture = sprite.Texture;
        if (!GodotObject.IsInstanceValid(texture))
            return new Vector2();
        var size = sprite.Texture.GetSize();
        var scale = sprite.GlobalScale;

        return new Vector2(size.X * scale.X, size.Y * scale.Y);
    }

    /// <summary>
    /// Sets the absolute scale of the sprite based on the desired size.
    /// </summary>
    /// <param name="sprite">The Sprite2D object.</param>
    /// <param name="size">The desired absolute size of the sprite.</param>
    public static void SetAbsoluteScale(this Sprite2D sprite, Vector2 size)
    {
        var textureSize = sprite.Texture.GetSize();
        sprite.Scale = new Vector2(size.X / textureSize.X, size.Y / textureSize.Y);
    }

}