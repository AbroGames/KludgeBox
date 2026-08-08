
using System;
using KludgeBox.DI;
using KludgeBox.Godot.Nodes;

namespace KludgeTests.Tests.GenerationTests;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PackedSceneContainerAttribute : Attribute
{
    internal string[] Include { get; private set; }
    internal string[] Exclude { get; private set; }

    public PackedSceneContainerAttribute(string[] include = null, string[] exclude = null)
    {
        Include = include ?? Array.Empty<string>();
        Exclude = exclude ?? Array.Empty<string>();
    }
}

[PackedSceneContainer(
    include: ["res://"]
    )]
public partial class ScenesContainerTest : CheckedAbstractStorage
{
    public override DependencyInjector GetDi()
    {
        throw new NotImplementedException();
    }
}