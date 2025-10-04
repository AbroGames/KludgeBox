using KludgeBox.Reflection.Access;

namespace KludgeBox.DI.Requests.ParentInjection;

public class ParentInjectionRequestScanner : IProcessingRequestScanner
{
    public bool TryGetRequest(IMemberAccessor accessor, out IProcessingRequest injectionRequest)
    {
        if (!accessor.TryGetAttribute<ParentAttribute>(out var parentAttribute))
        {
            injectionRequest = null;
            return false;
        }
        
        if (parentAttribute.SearchBy is By.Name)
        {
            injectionRequest = GetByNameRequest(accessor, parentAttribute);
            return true;
        }

        if (parentAttribute.SearchBy is By.Type)
        {
            injectionRequest = GetByTypeRequest(accessor, parentAttribute);
            return true;
        }

        injectionRequest = new ParentInjectionRequestByType(accessor, parentAttribute.DeepScan);
        return true;
    }
    
    private IProcessingRequest GetByNameRequest(IMemberAccessor accessor, ParentAttribute attribute)
    {
        var name = attribute.Name ?? accessor.Member.Name;
        var request = new ParentInjectionRequestByName(accessor, name);
        
        return request;
    }

    private IProcessingRequest GetByTypeRequest(IMemberAccessor accessor, ParentAttribute attribute)
    {
        var request = new ParentInjectionRequestByType(accessor, attribute.DeepScan);
        
        return request;
    }
}