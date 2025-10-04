using Godot;

namespace KludgeBox.Godot.Nodes;

public partial class Background : TextureRect
{
    public override void _Process(double delta)
    {
        Camera2D camera = GetViewport().GetCamera2D();

        // Get the size of the camera's viewport
        Vector2 cameraSize = camera.GetViewportRect().Size / camera.Zoom;
        // Get the camera's position in the world
        Vector2 cameraCenterPosition = camera.GlobalPosition;
        // Calculate the top-left corner position of the camera
        Vector2 cameraPosition = cameraCenterPosition - cameraSize / 2; 
        // Get the size of one tile of the texture
        Vector2 textureSize = Texture.GetSize();
        
        // Offset the texture based on the camera's position
        GlobalPosition = cameraPosition - cameraPosition.PosMod(textureSize);
        // Set the texture size to be equal to the screen size plus extra on each side
        Size = cameraSize + textureSize * 2;
    }
}