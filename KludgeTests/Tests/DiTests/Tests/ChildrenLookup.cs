using KludgeBox.DI;
using KludgeBox.Testing;
using static KludgeTests.Tests.DiTests.LocalServices;

namespace KludgeTests.Tests.DiTests;

public partial class ChildrenLookup : TestNode
{
    public override string TestName => "DI Tests: children injection";
    
    [Test]
    public void LookupAndInjectAllChildren()
    {
        var parent = BranchFactory.BuildValidInjectionsBranch();
        Di.Process(parent);
    }
}