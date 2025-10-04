using KludgeBox.DI.Requests;
using KludgeBox.Reflection.Access;

namespace KludgeBox.DI;

public class DependencyInjector
{
    public DependencyInjector() : this(new MembersScanner(), RequestsScanner.CreateDefault())
    {
        
    }
    
    public DependencyInjector(RequestsScanner requestsScanner) : this(new MembersScanner(), requestsScanner){}

    public DependencyInjector(MembersScanner membersScanner, RequestsScanner requestsScanner)
    {
        MembersScanner = membersScanner;
        RequestsScanner = requestsScanner;
    }

    public MembersScanner MembersScanner { get; set; }
    public RequestsScanner RequestsScanner { get; set; }

    private static Dictionary<Type, InjectableTypeInfo> _injectableTypeInfos = new();
    public void Process(object instance)
    {
        var type = instance.GetType();
        var injectableType = GetInjectableTypeInfo(type);
        
        var requests = injectableType.Requests;
        foreach (var request in requests)
        {
            try
            {
                request.ProcessOnInstance(instance);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    private InjectableTypeInfo GetInjectableTypeInfo(Type type)
    {
        if (_injectableTypeInfos.TryGetValue(type, out InjectableTypeInfo typeInfo))
        {
            return typeInfo;
        }

        var members = MembersScanner.ScanMembers(type);
        var requests = RequestsScanner.ScanRequests(members);
        
        typeInfo = new InjectableTypeInfo(type, requests);
        _injectableTypeInfos[type] = typeInfo;
        
        return typeInfo;
    }
}