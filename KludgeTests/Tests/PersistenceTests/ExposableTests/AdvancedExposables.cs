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
    
    [Test]
    public void SaveAndRestoreREfExposablesList()
    {
        var exposable = new BasicExposableWithListOfRefExposables(exposables: [
            new ReferenceExposable("SAMPLE_TEXT"),
            new ReferenceExposable("LOREM IPSUM")
        ]);
        
        var container = new JsonPersistenceContainer();
        var stream = container.Save(exposable);
        stream.Position = 0;
        Assert.AreNotEqual(0, stream.Length, "Save stream is empty");
        
        container = new JsonPersistenceContainer();
        var restored = container.Load<BasicExposableWithListOfRefExposables>(stream);
        Assert.IsNotNull(container, "Restored IRefExposable is null");
        Assert.AreEqual(exposable.Exposables1.Count, restored.Exposables1.Count, 
            $"Original Exposables1 count ({exposable.Exposables1.Count}) are not equal to restored Exposables1 count {restored.Exposables1.Count}");
        Assert.AreEqual(exposable.Exposables2.Count, restored.Exposables2.Count, 
            $"Original Exposables2 count ({exposable.Exposables2.Count}) are not equal to restored Exposables2 count {restored.Exposables2.Count}");
        
        for (int i = 0; i < exposable.Exposables1.Count; i++)
        {
            Assert.AreEqual(exposable.Exposables1[i].SomeStringValue, restored.Exposables1[i].SomeStringValue);
            Assert.AreEqual(exposable.Exposables2[i].SomeStringValue, restored.Exposables2[i].SomeStringValue);
            Assert.RefsAreEqual(restored.Exposables1[i], restored.Exposables2[i]);
        }
    }
}