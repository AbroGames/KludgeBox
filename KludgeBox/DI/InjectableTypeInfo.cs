using KludgeBox.DI.Requests;

namespace KludgeBox.DI;

public sealed class InjectableTypeInfo
{
    public Type Type { get; }
    public IReadOnlyList<IProcessingRequest> Requests => _requests;

    private List<IProcessingRequest> _requests;
    
    public InjectableTypeInfo(Type injectableType, List<IProcessingRequest> requests)
    {
        Type = injectableType;
        _requests = requests;
    }
    
}