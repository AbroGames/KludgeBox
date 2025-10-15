using System.Reflection;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace KludgeBox.Godot.Nodes.MpSync;

/// <summary>
/// This is a MultiplayerSynchronizer that, in its constructor (Node observableNode), automatically scans all properties and fields of the observableNode marked with the Sync attribute and adds them to the SceneReplicationConfig as synchronized.
/// This node cannot be added from the editor, as it would not work correctly with the MultiplayerSpawner in that case. The SceneReplicationConfig must be set up before _EnterTree().
/// This means you should either configure it through the editor (in which case you should use the regular MultiplayerSynchronizer) or call the constructor (Node observableNode) before AddChild(attributeMultiplayerSynchronizer). This is our case, and it only works from code.
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
        List<MemberInfo> syncedMembers = GetSyncedMembers(observableNode);
        syncedMembers.ForEach(AddMemberToReplicationConfig);
    }

    public override void _Ready()
    {
        SetRootPath(GetPathTo(_observableNode));
    }

    private List<MemberInfo> GetSyncedMembers(Node observableNode)
    {
        Type type = observableNode.GetType();
        List<MemberInfo> result = new();
        
        
        List<MemberInfo> members = new();
        BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        members.AddRange(type.GetProperties(bindingFlags));
        members.AddRange(type.GetFields(bindingFlags));

        foreach (MemberInfo member in members)
        {
            
            if (Attribute.IsDefined(member, typeof(SyncAttribute)))
            {
                if (!Attribute.IsDefined(member, typeof(ExportAttribute)))
                {
                    _log.Error($"{member.MemberType} '{member.Name}' in type '{observableNode.GetType()}' has Sync attribute, but doesn't have Export attribute");
                }
                else
                {
                    result.Add(member);
                }
            }
        }

        return result;
    }

    private void AddMemberToReplicationConfig(MemberInfo member)
    {
        SyncAttribute syncAttr = member.GetCustomAttribute<SyncAttribute>();
        if (syncAttr == null)
        {
            _log.Error($"Can't add {member.MemberType} '{member.Name}' to ReplicationConfig of '{GetPath()}' because it don't have Sync attribute");
            return;
        }
        
        string pathToNode = ".";
        string nodeAndMemberSeparator = ":";
        string memberName = member.Name;
        string fullPropertyName = pathToNode + nodeAndMemberSeparator + memberName;
        
        GetReplicationConfig().AddProperty(fullPropertyName);
        GetReplicationConfig().PropertySetReplicationMode(fullPropertyName, syncAttr.ReplicationMode);
        GetReplicationConfig().PropertySetSpawn(fullPropertyName, syncAttr.Spawn);
    }
}