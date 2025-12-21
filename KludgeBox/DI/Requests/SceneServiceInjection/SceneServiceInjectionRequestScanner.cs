using Godot;
using KludgeBox.Logging;
using KludgeBox.Reflection.Access;
using Serilog;

namespace KludgeBox.DI.Requests.SceneServiceInjection;

public class SceneServiceInjectionRequestScanner : IProcessingRequestScanner
{
    
    private readonly ILogger _log = LogFactory.GetForStatic<SceneServiceInjectionRequestScanner>();
    public bool TryGetRequest(IMemberAccessor accessor, out IProcessingRequest injectionRequest)
    {
        if (!accessor.TryGetAttribute<SceneServiceAttribute>(out var attribute))
        {
            injectionRequest = null;
            return false;
        }

        if (!accessor.Member.ReflectedType.IsAssignableTo(typeof(Node)))
        {
            injectionRequest = null;
            return false;
        }
        
        injectionRequest = new SceneServiceInjectionRequest(accessor,attribute.ServiceType);
        return true;
    }
}