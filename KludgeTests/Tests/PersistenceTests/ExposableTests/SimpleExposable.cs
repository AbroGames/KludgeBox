using System;
using System.Collections.Generic;
using Godot;
using KludgeBox.Testing;
using KludgeBox.Testing.Asserting;
using Persistence;
using Persistence.Json;
using static KludgeTests.Tests.LocalServices;

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
}

