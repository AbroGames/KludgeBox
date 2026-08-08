using Godot;
using KludgeBox.Godot.Nodes.MpSync;
using KludgeBox.Logging;
using KludgeBox.Reflection.Access;
using Serilog;

namespace KludgeBox.DI.Requests.MpSyncInjection;

public class MpSyncInjectionRequestScanner : IProcessingRequestScanner
{

    private readonly ILogger _log = LogFactory.GetForStatic<MpSyncInjectionRequestScanner>();
    
    public bool TryGetRequest(IMemberAccessor accessor, out IProcessingRequest injectionRequest)
    {
        if (!accessor.TryGetAttribute<SyncAttribute>(out _))
        {
            injectionRequest = null;
            return false;
        }
        
        if (!accessor.HasAttribute(typeof(ExportAttribute)))
        {
            _log.Error("Member has Sync attribute, but doesn't have Export attribute: {type}.{member}.",
                accessor.Member.ReflectedType?.FullName,
                accessor.Member.Name);
            injectionRequest = null;
            return false;
        }
        
        if (accessor.Member.DeclaringType == null || !accessor.Member.DeclaringType.IsAssignableTo(typeof(Node)))
        {
            _log.Error("Sync attribute at not Node class: {type}.{member}.",
                accessor.Member.ReflectedType?.FullName,
                accessor.Member.Name);
            injectionRequest = null;
            return false;
        }

        injectionRequest = new MpSyncInjectionRequest(accessor);
        return true;
    }
}