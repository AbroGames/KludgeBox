using KludgeBox.Reflection.Access;

namespace KludgeBox.DI.Requests.ChildInjection;



public class ChildInjectionRequestScanner : IProcessingRequestScanner
{
    public bool TryGetRequest(IMemberAccessor accessor, out IProcessingRequest injectionRequest)
    {
        if (!accessor.TryGetAttribute<ChildAttribute>(out var importAttribute))
        {
            injectionRequest = null;
            return false;
        }
        
        if (importAttribute.SearchBy is By.Name)
        {
            injectionRequest = GetByNameRequest(accessor, importAttribute);
            return true;
        }

        if (importAttribute.SearchBy is By.Type)
        {
            injectionRequest = GetByTypeRequest(accessor, importAttribute);
            return true;
        }

        throw new Exception($"Supposedly unreachable code has been reached while scanning for child injection request for {accessor.Member.ReflectedType?.FullName}.{accessor.Member.Name}");
    }

    private IProcessingRequest GetByNameRequest(IMemberAccessor accessor, ChildAttribute attribute)
    {
        var name = attribute.Name ?? accessor.Member.Name;
        var request = new ChildInjectionRequestByName(accessor, name, attribute.DeepScan);
        
        return request;
    }

    private IProcessingRequest GetByTypeRequest(IMemberAccessor accessor, ChildAttribute attribute)
    {
        var request = new ChildInjectionRequestByType(accessor, attribute.DeepScan);
        
        return request;
    }
}