using System;

namespace KludgeBox.SourceGenerators;

internal sealed record SceneModel : IEquatable<SceneModel>
{
    public SceneModel(string propertyName,    // "Player" из "Player.tscn"
        string resourcePath,    // "res://Game/Characters/Player.tscn"
        string ns,       // для генерации кода
        string className)
    {
        PropertyName = propertyName;
        ResourcePath = resourcePath;
        Namespace = ns;
        ClassName = className;
    }

    public bool Equals(SceneModel? other) =>
        other is not null &&
        PropertyName == other.PropertyName &&
        ResourcePath == other.ResourcePath &&
        Namespace == other.Namespace &&
        ClassName == other.ClassName;

    public override int GetHashCode() =>
        HashCode.Combine(PropertyName, ResourcePath, Namespace, ClassName);

    public string PropertyName { get; }
    public string ResourcePath { get; }
    public string Namespace { get; }
    public string ClassName { get; }

    public void Deconstruct(out string propertyName,    // "Player" из "Player.tscn"
        out string resourcePath,    // "res://Game/Characters/Player.tscn"
        out string ns,       // для генерации кода
        out string className)
    {
        propertyName = PropertyName;
        resourcePath = ResourcePath;
        ns = Namespace;
        className = ClassName;
    }
}