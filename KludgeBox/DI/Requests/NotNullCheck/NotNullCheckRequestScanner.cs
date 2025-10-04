using KludgeBox.Reflection.Access;

namespace KludgeBox.DI.Requests.NotNullCheck;

public class NotNullCheckRequestScanner : IProcessingRequestScanner
{
    private Type[] _notNullAttributeTypes;
    
    public NotNullCheckRequestScanner() : this(typeof(NotNullAttribute)) {}

    public NotNullCheckRequestScanner(params Type[] notNullAttributeTypes)
    {
        _notNullAttributeTypes = notNullAttributeTypes;
    }
    public bool TryGetRequest(IMemberAccessor accessor, out IProcessingRequest injectionRequest)
    {
        if (accessor.ValueType.IsValueType)
        {
            injectionRequest = null;
            return false;
        }
        
        var attributeTypes = accessor.Attributes.Select(a => a.GetType());
        var notNullAttributeTypes = attributeTypes.Where(attr => _notNullAttributeTypes.Any(attr.IsAssignableTo)).ToArray();
        
        if (notNullAttributeTypes.Any())
        {
            bool throwOnFail = false;

            if (accessor.TryGetAttribute<NotNullAttribute>(out var notNullAttribute))
            {
                throwOnFail = notNullAttribute.ThrowOnFail;
            }
            
            injectionRequest = new NotNullCheckRequest(accessor, throwOnFail);
            return true;
        }
        
        injectionRequest = null;
        return false;
    }
}