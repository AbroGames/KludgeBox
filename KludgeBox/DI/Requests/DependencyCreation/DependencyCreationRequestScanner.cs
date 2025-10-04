using KludgeBox.Reflection.Access;

namespace KludgeBox.DI.Requests.DependencyCreation;

public class DependencyCreationRequestScanner : IProcessingRequestScanner
{
    public bool TryGetRequest(IMemberAccessor accessor, out IProcessingRequest injectionRequest)
    {
        if (!accessor.TryGetAttribute<CreateAttribute>(out var importAttribute))
        {
            injectionRequest = null;
            return false;
        }

        var request = new DependencyCreationRequest(accessor);
        injectionRequest = request;
        return true;
        //throw new Exception($"Supposedly unreachable code has been reached while scanning for child injection request for {accessor.Member.ReflectedType?.FullName}.{accessor.Member.Name}");
    }

    
}