using KludgeBox.DI;
using KludgeBox.Testing;
using static KludgeTests.Tests.DiTests.LocalServices;

namespace KludgeTests.Tests.DiTests;

[TestGroup("DI Tests")]
public partial class ChildrenLookup : TestNode
{
    public override string TestName => "Children injection";
    
    [Test]
    public void LookupAndInjectAllChildren()
    {
        var parent = BranchFactory.BuildValidInjectionsBranch();
        Di.Process(parent);
    }
}