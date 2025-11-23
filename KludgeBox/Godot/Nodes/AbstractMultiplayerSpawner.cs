using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.DI.Requests.NotNullCheck;
using Serilog;

namespace KludgeBox.Godot.Nodes;


/// <summary>
/// <b>To use this, you must inherit this class and create a scene.</b><br/>
/// Godot cannot attach code from libraries to scenes; it requires a <c>*.cs</c> file within the main project.<br/>
/// It is sufficient to simply create a class inheriting from this one, without any changes.<br/>
/// <br/>
/// To use the class, you need to create a scene of type <see cref="MultiplayerSpawner"/>, for example, <c>WorldMultiplayerSpawner</c>.<br/>
/// Attach the <c>*.cs</c> file created in the previous step to the scene node.<br/>
/// <br/>
/// The scene must have two child nodes: <c>PackedScenes</c> (type <see cref="Node"/>) and <c>MultiplayerSynchronizer</c> (type <see cref="MultiplayerSynchronizer"/>).<br/>
/// <c>PackedScenes</c> is a reference to another scene (inheriting <see cref="AbstractStorage"/>) that contains all nodes to be spawned. <b>It must not contain the <c>WorldMultiplayerSpawner</c> scene itself</b>!<br/>
/// In <c>MultiplayerSynchronizer</c>, the <c>RootPath</c> must be set to <c>WorldMultiplayerSpawner</c>, and one property added for synchronization: <c>spawn_path</c> with settings <c>Spawn=true</c> and <c>Replicate=Never</c>.<br/>
/// In <c>WorldMultiplayerSpawner</c>, the reference to the child node <c>PackedScenes</c> must be assigned.<br/>
/// <br/>
/// Code example for <c>WorldMultiplayerSpawner</c>:
/// <code>
/// public partial class WorldMultiplayerSpawner : AbstractMultiplayerSpawner
/// {
///     [Export] [NotNull] public WorldPackedScenes PackedScenes { get; set; }
///     [Export] private bool _selfSync = true;
///     
///     public override IReadOnlyList&lt;PackedScene&gt; GetPackedScenesForSpawn()
///     {
///         return PackedScenes.GetScenesList();
///     }
/// 
///     public override bool GetSelfSync()
///     {
///         return _selfSync;
///     }
/// 
///     public override void SetSelfSync(bool selfSync)
///     {
///         _selfSync = selfSync;
///     }
/// }
/// </code>
/// </summary>
public abstract partial class AbstractMultiplayerSpawner : MultiplayerSpawner
{
    
    private Node _observableNode;
    [Logger] private ILogger _log;
    
    public AbstractMultiplayerSpawner InitPreReady(Node observableNode, bool selfSync = true)
    {
        Di.Process(this);
        
        _observableNode = observableNode;
        SetSelfSync(selfSync);
        return this;
    }

    public override void _Ready()
    {
        foreach (var packedScene in GetPackedScenesForSpawn())
        {
            AddSpawnableScene(packedScene.ResourcePath);
        }
        if (GetSelfSync()) 
        {
            AddSpawnableScene(SceneFilePath); // Reference by self
        }
        
        // _observableNode can be null if the Spawner is synced over the network by another Spawner or created in the Editor.
        // In these cases, SpawnPath must not be null.
        if (_observableNode != null) 
        {
            SetSpawnPath(GetPathTo(_observableNode));
        }
        else if (string.IsNullOrEmpty(GetSpawnPath())) 
        {
            _log.Error("AbstractMultiplayerSpawner must have not null _observableNode or SpawnPath. Spawner path: {path}", GetPath());
        }
    }

    public abstract IReadOnlyList<PackedScene> GetPackedScenesForSpawn();
    public abstract bool GetSelfSync();
    public abstract void SetSelfSync(bool selfSync);
}