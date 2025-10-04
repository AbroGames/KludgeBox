using Godot;
using KludgeBox.Reflection.Access;

namespace KludgeBox.DI.Requests.DependencyCreation;

public class DependencyCreationRequest : IProcessingRequest
{
    private readonly IMemberAccessor _accessor;

    public DependencyCreationRequest(IMemberAccessor accessor)
    {
        _accessor = accessor;
    }
    
    public void ProcessOnInstance(object instance)
    {
        var dependencyType = _accessor.ValueType;
        
        var dependencyInstance = Activator.CreateInstance(dependencyType, null);
        _accessor.SetValue(instance, dependencyInstance);

        if (instance is Node parent && dependencyInstance is Node child)
        {
            parent.AddChild(child);
        }
    }
}