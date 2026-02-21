using System;
using KludgeBox.DI.Requests.SceneServiceInjection;
using KludgeBox.Testing;
using KludgeBox.Testing.Asserting;
using static KludgeTests.Tests.LocalServices;

namespace KludgeTests.Tests.DiTests;

[TestGroup("DI Tests")]
public partial class SceneServiceInjection : TestNode
{
    public override string TestName => "Scene services injection";

    [Test]
    public void InjectSimpleService()
    {
        var serviceParent = new ServiceProviderParent();
        var someOtherServiceParent = new SomeOtherServiceProviderParent();
        var child = new ChildNodeWithServices();
        
        someOtherServiceParent.AddChild(serviceParent);
        serviceParent.AddChild(child);
        
        Di.Process(child);
        
        Assert.IsNotNull(child.Service);
        Assert.IsNotNull(child.SomeOtherService);
    }

    internal partial class ChildNodeWithServices : DiTestTypes.ChildNode
    {
        [SceneService] public SomeService Service { get; private set; }
        [SceneService] public SomeOtherService SomeOtherService { get; private set; }
    }
    
    internal partial class ServiceProviderParent : DiTestTypes.ParentNode,  IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(SomeService))
            {
                return new SomeService();
            }

            return null;
        }
    }
    
    internal partial class SomeOtherServiceProviderParent : DiTestTypes.ParentNode,  IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(SomeOtherService))
            {
                return new SomeOtherService();
            }

            return null;
        }
    }

    internal class SomeService;
    internal class SomeOtherService;
}