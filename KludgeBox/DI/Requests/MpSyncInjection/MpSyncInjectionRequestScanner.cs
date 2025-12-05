using KludgeBox.Godot.Nodes.MpSync;
using KludgeBox.Reflection.Access;

namespace KludgeBox.DI.Requests.MpSyncInjection;

public class MpSyncInjectionRequestScanner : IProcessingRequestScanner
{
    public bool TryGetRequest(IMemberAccessor accessor, out IProcessingRequest injectionRequest)
    {
        if (!accessor.TryGetAttribute<SyncAttribute>(out _))
        {
            injectionRequest = null;
            return false;
        }

        injectionRequest = new MpSyncInjectionRequest(accessor);
        return true;
    }
}