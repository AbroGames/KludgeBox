using KludgeBox.Testing;
using KludgeBox.Testing.Asserting;
using Persistence.Json;

namespace KludgeTests.Tests.PersistenceTests.ExposableTests;

[TestGroup("Exposable Tests")]
public partial class AdvancedExposable : TestNode
{
    public override string TestName => "Advanced Exposable";

    [Test]
    public void SaveAndLoadExposablesWithNestedReferences()
    {
        var original = new ExposableWithNestedReferences(new ReferenceExposable("SAMPLE_TEXT"));
        var container = new JsonPersistenceContainer();
        
        var stream = container.Save(original);
        stream.Position = 0;
        Assert.AreNotEqual(0, stream.Length, "Save stream is empty");
        
        container = new JsonPersistenceContainer();
        var restored = container.Load<ExposableWithNestedReferences>(stream);
        
        Assert.IsNotNull(container, "Restored IExposable is null");
        Assert.IsNotNull(restored.Reference1, "Restored Reference1 is null");
        Assert.IsNotNull(restored.Reference2, "Restored Reference2 is null");
        
        Assert.IsTrue(restored.Reference1 == restored.Reference2, "Restored Reference1 is not the same instance as Reference2");
        restored.Reference1.SomeStringValue = "LOREM IPSUM";
        Assert.AreEqual(restored.Reference1.SomeStringValue, restored.Reference2.SomeStringValue, "Expected to have changed value in both references, but they are different");
    }

    [Test]
    public void ReusePersistenceContainerToSaveAndLoadExposablesWithNestedReferences()
    {
        var original = new ExposableWithNestedReferences(new ReferenceExposable("SAMPLE_TEXT"));
        var container = new JsonPersistenceContainer();
        
        var stream = container.Save(original);
        stream.Position = 0;
        Assert.AreNotEqual(0, stream.Length, "Save stream is empty");
        
        var restored = container.Load<ExposableWithNestedReferences>(stream);
        
        Assert.IsNotNull(container, "Restored IExposable is null");
        Assert.IsNotNull(restored.Reference1, "Restored Reference1 is null");
        Assert.IsNotNull(restored.Reference2, "Restored Reference2 is null");
        
        Assert.IsTrue(restored.Reference1 == restored.Reference2, "Restored Reference1 is not the same instance as Reference2");
        restored.Reference1.SomeStringValue = "LOREM IPSUM";
        Assert.AreEqual(restored.Reference1.SomeStringValue, restored.Reference2.SomeStringValue, "Expected to have changed value in both references, but they are different");
        
    }
}