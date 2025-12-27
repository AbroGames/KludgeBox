using KludgeBox.DI.Requests;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.ParentInjection;
using KludgeBox.Testing;
using KludgeBox.Testing.Asserting;
using static KludgeTests.Tests.LocalServices;

namespace KludgeTests.Tests.DiTests;

[TestGroup("DI Tests")]
public partial class ParentLookup : TestNode
{
    public override string TestName => "Parent injection";

    [Test]
    public void InjectParentToTheChild()
    {
        var deepParent = new DeepParentNode();
        var parent = new DiTestTypes.ParentNode();
        var child = new ChildNodeWithParentReference();
        
        deepParent.AddChild(parent);
        parent.AddChild(child);
        parent.Name = "ParentByName";
        
        Di.Process(child);
        
        Assert.IsNotNull(child.ParentByName, $"Parent was not found by name");
        Assert.IsNotNull(child.DeepParentByType, $"Parent was not found by type");
    }

    internal partial class ChildNodeWithParentReference : DiTestTypes.ChildNode
    {
        [Parent(By.Name)]public DiTestTypes.ParentNode ParentByName { get; private set; }
        [Parent(By.Type, true)]public DeepParentNode DeepParentByType { get; private set; }
    }

    internal partial class DeepParentNode : DiTestTypes.ParentNode;
}