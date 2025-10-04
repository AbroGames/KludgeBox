using Godot;

namespace KludgeBox.Godot.Nodes.Camera.Shifts;

public class Punch(Vector2 rotation, double strength, double movementSpeed = 3000) : IShiftProvider
{
    public Vector2 Rotation { get; private set; } = rotation.Normalized();
    public double Strength { get; private set; } = strength;
    public double InitialStrength { get; private set; } = strength;

    public Vector2 Shift => Rotation * (float) Strength;
    public bool IsAlive => Strength > Mathf.Epsilon;
    public void Update(double delta)
    {
        Strength = Mathf.Max(0, Strength - movementSpeed * delta);
    }
}
