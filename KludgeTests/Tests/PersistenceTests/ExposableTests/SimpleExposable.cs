using Godot;
using KludgeBox.Persistence.Exposables.Json;
using KludgeBox.Testing;
using KludgeBox.Testing.Asserting;

namespace KludgeTests.Tests.PersistenceTests.ExposableTests;

[TestGroup("Exposable Tests")]
public partial class SimpleExposable : TestNode
{
    public override string TestName => "Simple Exposable";
    
    [Test]
    public void SaveAndRestoreSimpleExposable()
    {
        var original = new BasicExposable(10, 20, "SAMPLE_TEXT", new (8, 800));
        var container = new JsonPersistenceContainer();
        
        var stream = container.Save(original);
        stream.Position = 0;
        Assert.AreNotEqual(0, stream.Length, "Save stream is empty");
        
        container = new JsonPersistenceContainer();
        var restored = container.Load<BasicExposable>(stream);
        
        Assert.IsNotNull(container, "Restored IExposable is null");
        Assert.AreEqual(original, restored);
    }

    [Test]
    public void SaveAndRestoreNestedExposable()
    {
        var originalBasic = new BasicExposable(10, 20, "SAMPLE_TEXT", new (8, 800));
        var original = new NestedExposable(originalBasic, 100, 200, "LOREM IPSUM", new Vector2(555, 3535));
        
        var container = new JsonPersistenceContainer();
        var stream = container.Save(original);
        stream.Position = 0;
        Assert.AreNotEqual(0, stream.Length, "Save stream is empty");
        
        container = new JsonPersistenceContainer();
        var restored = container.Load<NestedExposable>(stream);
        
        Assert.IsNotNull(container, "Restored IExposable is null");
        Assert.AreEqual(original, restored);
    }

    [Test]
    public void SaveAndRestoreValuesList()
    {
        var exposable = new BasicExposableWithListsOfValues(
            strings: ["SAMPLE_TEXT", "LOREM IPSUM"],
            integers: [8, 800],
            vectors: [new(555, 3535), new (0, 1)]);
        
        var container = new JsonPersistenceContainer();
        var stream = container.Save(exposable);
        stream.Position = 0;
        Assert.AreNotEqual(0, stream.Length, "Save stream is empty");
        
        container = new JsonPersistenceContainer();
        var restored = container.Load<BasicExposableWithListsOfValues>(stream);
        Assert.IsNotNull(container, "Restored IExposable is null");
        Assert.AreEqual(exposable.Strings.Count, restored.Strings.Count, 
            $"Original strings count ({exposable.Strings.Count}) are not equal to restored strings count {restored.Strings.Count}");
        Assert.AreEqual(exposable.Integers.Count, restored.Integers.Count,
            $"Original integers count ({exposable.Integers.Count}) are not equal to restored integers count {restored.Integers.Count}");
        Assert.AreEqual(exposable.Vectors.Count, restored.Vectors.Count,
            $"Original vectors count ({exposable.Vectors.Count}) are not equal to restored vectors count {restored.Vectors.Count}");
        
        for (int i = 0; i < exposable.Strings.Count; i++)
        {
            Assert.AreEqual(exposable.Strings[i], restored.Strings[i]);
            Assert.AreEqual(exposable.Integers[i], restored.Integers[i]);
            Assert.AreEqual(exposable.Vectors[i], restored.Vectors[i]);
        }
    }
    
    [Test]
    public void SaveAndRestoreExposablesList()
    {
        var exposable = new BasicExposableWithListOfExposables(exposables: [
            new BasicExposable(1, 2, "3", new Vector2(4,5)),
            new BasicExposable(6, 7, "8", new Vector2(9,10))
        ]);
        
        var container = new JsonPersistenceContainer();
        var stream = container.Save(exposable);
        stream.Position = 0;
        Assert.AreNotEqual(0, stream.Length, "Save stream is empty");
        
        container = new JsonPersistenceContainer();
        var restored = container.Load<BasicExposableWithListOfExposables>(stream);
        Assert.IsNotNull(container, "Restored IExposable is null");
        Assert.AreEqual(exposable.Exposables.Count, restored.Exposables.Count, 
            $"Original Exposables count ({exposable.Exposables.Count}) are not equal to restored Exposables count {restored.Exposables.Count}");
        
        for (int i = 0; i < exposable.Exposables.Count; i++)
        {
            Assert.AreEqual(exposable.Exposables[i], restored.Exposables[i]);
        }
    }

    [Test]
    public void SaveAndLoadDictionaryOfValues()
    {
        var exposable = new BasicExposableWithDictionaryOfValues( new (){ 
            { "one", new(1, 2) },
            { "two", new(3, 4) },
            { "three", new(5, 6) }
        });
        var container = new JsonPersistenceContainer();
        var stream = container.Save(exposable);
        stream.Position = 0;
        Assert.AreNotEqual(0, stream.Length, "Save stream is empty");
        
        container = new JsonPersistenceContainer();
        var restored = container.Load<BasicExposableWithDictionaryOfValues>(stream);
        Assert.IsNotNull(container, "Restored IExposable is null");
        
        Assert.AreEqual(exposable.Str2Vec2.Count, restored.Str2Vec2.Count, 
            $"Original pairs count ({exposable.Str2Vec2.Count}) are not equal to restored pairs count {restored.Str2Vec2.Count}");

        foreach (var (key, value) in exposable.Str2Vec2)
        {
            Assert.IsTrue(restored.Str2Vec2.TryGetValue(key, out var restoredValue));
            Assert.AreEqual(value, restoredValue);
        }
    }
    
    [Test]
    public void SaveAndLoadDictionaryOfExposables()
    {
        var exposable = new BasicExposableWithDictionaryOfExposables( new (){ 
            { Color.FromHtml("ff0000"), new BasicExposable(1, 2, "3", new Vector2(4,5)) },
            { Color.FromHtml("00ff00"), new BasicExposable(6, 7, "8", new Vector2(9,10)) },
            { Color.FromHtml("0000ff"), new BasicExposable(7, 8, "9", new Vector2(11,12)) }
        });
        var container = new JsonPersistenceContainer();
        var stream = container.Save(exposable);
        stream.Position = 0;
        Assert.AreNotEqual(0, stream.Length, "Save stream is empty");
        
        container = new JsonPersistenceContainer();
        var restored = container.Load<BasicExposableWithDictionaryOfExposables>(stream);
        Assert.IsNotNull(container, "Restored IExposable is null");
        
        Assert.AreEqual(exposable.Col2Exposable.Count, restored.Col2Exposable.Count, 
            $"Original pairs count ({exposable.Col2Exposable.Count}) are not equal to restored pairs count {restored.Col2Exposable.Count}");

        foreach (var (key, value) in exposable.Col2Exposable)
        {
            Assert.IsTrue(restored.Col2Exposable.TryGetValue(key, out var restoredValue));
            Assert.AreEqual(value, restoredValue);
        }
    }
}

