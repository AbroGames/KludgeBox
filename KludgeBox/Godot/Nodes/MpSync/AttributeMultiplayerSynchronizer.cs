using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.Reflection.Access;
using Serilog;

namespace KludgeBox.Godot.Nodes.MpSync;

/// <summary>
/// <b>This node cannot be added from the editor</b><br/>
/// This is a <see cref="MultiplayerSynchronizer"/>,
/// which in its constructor <c>AttributeMultiplayerSynchronizer(Node observableNode)</c>,
/// automatically scans all properties and fields of the <c>observableNode</c>,
/// marked with the <see cref="SyncAttribute"/> and adds them to the <see cref="SceneReplicationConfig"/> as synchronized.<br/>
/// <br/>
/// This node cannot be added from the editor, as it would not work correctly with the <see cref="MultiplayerSpawner"/> in that case.
/// Because the <see cref="SceneReplicationConfig"/> must be set up before <c>_EnterTree()</c>.<br/>
/// <br/>
/// This means you should either configure it through the editor (in which case you should use the regular <see cref="MultiplayerSynchronizer"/>),
/// or call the constructor <c>AttributeMultiplayerSynchronizer(Node observableNode)</c> before <c>AddChild(attributeMultiplayerSynchronizer)</c>.
/// This is our case, and it only works from code.
/// </summary>
// ReSharper disable once Godot.MissingParameterlessConstructor
public partial class AttributeMultiplayerSynchronizer : MultiplayerSynchronizer
{

    private Node _observableNode;
    
    [Logger] private ILogger _log;
    
    public AttributeMultiplayerSynchronizer(Node observableNode)
    {
        Di.Process(this);
        
        if (observableNode == null)
        {
            _log.Error("AttributeMultiplayerSynchronizer must have not null _observableNode. Synchronizer path: " + GetPath());
            return;
        }
        _observableNode = observableNode;
        
        // Add all [Sync] properties and fields to ReplicationConfig.Property
        SetReplicationConfig(new SceneReplicationConfig());
        List<IMemberAccessor> syncedMembers = GetSyncedMembers(observableNode);
        syncedMembers.ForEach(AddMemberToReplicationConfig);
    }

    public override void _Ready()
    {
        SetRootPath(GetPathTo(_observableNode));
    }

    private List<IMemberAccessor> GetSyncedMembers(Node observableNode)
    {
        Type type = observableNode.GetType();
        List<IMemberAccessor> result = new();
        
        
        IReadOnlyList<IMemberAccessor> members = KludgeBoxServices.MembersScanner.ScanMembers(type);
        foreach (var member in members)
        {
            if (member.HasAttribute<SyncAttribute>())
            {
                if (member.HasAttribute<ExportAttribute>())
                {
                    result.Add(member);
                }
                else
                {
                    _log.Error($"{member.Member.MemberType} '{member.Member.Name}' in type '{observableNode.GetType()}' has Sync attribute, but doesn't have Export attribute");
                }
            }
        }

        return result;
    }

    private void AddMemberToReplicationConfig(IMemberAccessor member)
    {
        if (!member.TryGetAttribute<SyncAttribute>(out var syncAttr))
        {
            _log.Error($"Can't add {member.Member.MemberType} '{member.Member.Name}' to ReplicationConfig of '{GetPath()}' because it don't have Sync attribute");
            return;
        }
        
        string pathToNode = ".";
        string nodeAndMemberSeparator = ":";
        string memberName = member.Member.Name;
        string fullPropertyName = pathToNode + nodeAndMemberSeparator + memberName;
        
        GetReplicationConfig().AddProperty(fullPropertyName);
        GetReplicationConfig().PropertySetReplicationMode(fullPropertyName, syncAttr.ReplicationMode);
        GetReplicationConfig().PropertySetSpawn(fullPropertyName, syncAttr.Spawn);
    }
}