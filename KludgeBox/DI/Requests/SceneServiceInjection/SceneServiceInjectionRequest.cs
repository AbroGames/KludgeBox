using Godot;
using KludgeBox.Reflection.Access;

namespace KludgeBox.DI.Requests.SceneServiceInjection;

public class SceneServiceInjectionRequest : IProcessingRequest
{
    
    private readonly IMemberAccessor _memberAccessor;
    private readonly Type _serviceType;

    public SceneServiceInjectionRequest(IMemberAccessor memberAccessor, Type serviceType)
    {
        _memberAccessor = memberAccessor;
        _serviceType = serviceType ?? _memberAccessor.ValueType;
    }


    public void ProcessOnInstance(object instance)
    {
        var nodeInstance = (Node)instance; // All checks are done in scanner
        var service = GetService(_serviceType, nodeInstance);
        
        if (service is null)
        {
            throw new InvalidOperationException($"Service of type {_serviceType} not found for node {nodeInstance.GetPath()}@{nodeInstance.GetType().Name}.");
        }
        
        _memberAccessor.SetValue(instance, service);
    }
    
    private object GetService(Type serviceType, Node possibleServiceProvider)
    {
        object foundService = null;
        
        if (possibleServiceProvider is ISceneServiceProvider sceneServiceProvider)
        {
            foundService = sceneServiceProvider.GetService(serviceType);
        }
        
        if (possibleServiceProvider is IServiceProvider serviceProvider)
        {
            foundService = serviceProvider.GetService(serviceType);
        }
        
        if (foundService is not null)
        {
            return foundService;
        }
        
        var parent = possibleServiceProvider.GetParent();
        if (!parent.IsValid())
        {
            return null;
        }
        
        return GetService(serviceType, parent);
    }
}