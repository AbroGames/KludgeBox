using Godot;

namespace KludgeBox.Godot.Nodes.MpSync;

/// <summary>
/// See <see cref="AttributeMultiplayerSynchronizer"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SyncAttribute : Attribute
{
    
    public SceneReplicationConfig.ReplicationMode ReplicationMode { get; }
    public bool Spawn { get; }

    public SyncAttribute(SceneReplicationConfig.ReplicationMode replicationMode = SceneReplicationConfig.ReplicationMode.OnChange, bool spawn = true)
    {
        ReplicationMode = replicationMode;
        Spawn = spawn;
    }
}