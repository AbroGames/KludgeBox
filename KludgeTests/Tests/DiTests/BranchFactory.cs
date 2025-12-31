using Godot;

namespace KludgeTests.Tests.DiTests;

internal static class BranchFactory
{
    public static DiTestTypes.ParentNodeWithChildrenReferences BuildValidInjectionsBranch()
    {
        var parent = new DiTestTypes.ParentNodeWithChildrenReferences();
        parent.Add<DiTestTypes.ChildNode>(nameof(DiTestTypes.ParentNodeWithChildrenReferences.DefaultPublicChildProperty));
        parent.Add<DiTestTypes.ChildNode>("_defaultPrivateChildProperty");
        parent.Add<DiTestTypes.ChildNode>(nameof(DiTestTypes.ParentNodeWithChildrenReferences.DefaultPublicChildField));
        parent.Add<DiTestTypes.ChildNode>("_defaultPrivateChildField");
        parent.Add<DiTestTypes.ChildNode>(DiTestTypes.ParentNodeWithChildrenReferences.CustomChildName);
        parent.Add<DiTestTypes.SpecificChildNode>(nameof(DiTestTypes.ParentNodeWithChildrenReferences.ByTypeChildProperty));
        parent.Add<Node>("StubForDeepSearch")
            .Add<DiTestTypes.DeepSpecificChildNode>(nameof(DiTestTypes.ParentNodeWithChildrenReferences.DeepByTypeChildProperty))
            .Add<DiTestTypes.ChildNode>(nameof(DiTestTypes.ParentNodeWithChildrenReferences.DeepByNameChildField));
        
        return parent;
    }
    
    public static TNode Add<TNode>(this Node parent, string name) where TNode : Node, new()
    {
        var newNode = new TNode();
        parent.AddChild(newNode);
        newNode.Name = name;
        
        return newNode;
    }
}