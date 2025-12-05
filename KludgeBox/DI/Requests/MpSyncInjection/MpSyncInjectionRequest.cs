using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.Godot.Nodes.MpSync;
using KludgeBox.Reflection.Access;
using Serilog;

namespace KludgeBox.DI.Requests.MpSyncInjection;

public class MpSyncInjectionRequest : IProcessingRequest
{
    public static readonly string MpSyncNodeName = "MultiplayerSynchronizer";
    
    private readonly IMemberAccessor _memberAccessor;
    
    [Logger] private ILogger _log;

    public MpSyncInjectionRequest(IMemberAccessor memberAccessor)
    {
        Di.Process(this);
        
        _memberAccessor = memberAccessor;
    }

    public void ProcessOnInstance(object instance)
    {
        if (instance is Node node)
        {
            if (!_memberAccessor.HasAttribute(typeof(ExportAttribute)))
            {
                _log.Error("Member has Sync attribute, but doesn't have Export attribute: {type}.{member}.",
                    _memberAccessor.Member.ReflectedType?.FullName,
                    _memberAccessor.Member.Name);
                return;
            }

            var mpSync = node.GetNodeOrNull<AttributeMultiplayerSynchronizer>(MpSyncNodeName);
            if (mpSync == null)
            {
                mpSync = new AttributeMultiplayerSynchronizer(node);
                node.AddChildWithName(mpSync, MpSyncNodeName);
            }
        }
        else
        {
            _log.Error("Sync attribute at not Node class: {type}.{member}.",
                _memberAccessor.Member.ReflectedType?.FullName,
                _memberAccessor.Member.Name);
        }
    }
}