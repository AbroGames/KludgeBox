using KludgeBox.DI;

namespace KludgeBox.Godot.Nodes;

/// <summary>
/// Storage with auto calling <c>Di.Process(this)</c>.<br/>
/// <br/>
/// <b>You must add [NotNull] to every field that requires validation.</b>
/// </summary>
public abstract partial class CheckedAbstractStorage : AbstractStorage
{
    public override void _PreReady()
    {
        GetDi().Process(this);
    }

    public abstract DependencyInjector GetDi();
}