using System.Reflection;
using Godot;

namespace KludgeBox.Godot.Nodes;

public abstract partial class AbstractStorage : Node
{
    
    public IReadOnlyList<PackedScene> GetScenesList() => _scenesList.AsReadOnly();
    public IReadOnlyDictionary<string, PackedScene> GetScenesDictionary() => _scenes.AsReadOnly();
    
    private Dictionary<string, PackedScene> _scenes = new();
    private List<PackedScene> _scenesList = new();
    
    /// <summary>
    /// Called in sealed <see cref="AbstractStorage._Ready()"/> before scanning for scenes.
    /// </summary>
    public virtual void _PreReady()
    {
        
    }
    
    public sealed override void _Ready()
    {
        _PreReady();
        RegisterScenes(this);
    }
        
    public bool TryGetScene(string name, out PackedScene scene)
    {
        return _scenes.TryGetValue(name, out scene);
    }
    
    private void RegisterScenes(object obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        Type type = obj.GetType();
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!property.PropertyType.IsAssignableTo(typeof(PackedScene))) continue;
            if (!Attribute.IsDefined(property, typeof(ExportAttribute))) continue;

            var scene = property.GetValue(this) as PackedScene;
            _scenes[property.Name] = scene;
            _scenesList.Add(scene);
        }
    }
}