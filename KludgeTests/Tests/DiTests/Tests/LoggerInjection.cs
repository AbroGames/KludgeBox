using System.Collections.Generic;
using Godot;
using KludgeBox.DI;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.Testing;
using KludgeBox.Testing.Asserting;
using Serilog;
using static KludgeTests.Tests.DiTests.LocalServices;

namespace KludgeTests.Tests.DiTests;

public partial class LoggerInjection : TestNode
{
    public override string TestName => "DI Tests: logger injection";
    
    [Test]
    public void InjectLoggerToNode()
    {
        var node = new NodeWithLogger();
        Di.Process(node);
        var logger = node.Logger;
        logger.Information("Test log from NodeWithLogger");
        Assert.IsNotNull(logger, $"Failed to inject logger to {node.GetType().Name}");
    }

    [Test]
    public void InjectLoggerToNotNode()
    {
        var instance = new NotNodeWithLogger();
        Di.Process(instance);
        var logger = instance.Logger;
        logger.Information("Test log from NotNodeWithLogger");
        Assert.IsNotNull(logger,$"Failed to inject logger to {instance.GetType().Name}");
    }

    [Test]
    public void InjectMultipleLoggers()
    {
        var instance = new TypeWithMultipleLoggers();
        Di.Process(instance);
        var loggers = instance.GetLoggers();

        foreach (var (member, logger) in loggers)
        {
            Assert.IsNotNull(logger, $"Failed to inject logger to {instance.GetType().Name}:{member}");
            logger.Information("Test log from TypeWithMultipleLoggers:{member}", member);
        }
    }
}

internal partial class NodeWithLogger : Node
{
    [Logger] public ILogger Logger { get; private set; }
}

internal class NotNodeWithLogger
{
    [Logger] public ILogger Logger { get; private set; }
}

internal class TypeWithMultipleLoggers
{
    [Logger] public ILogger PublicLogger { get; set; }
    [Logger] private ILogger _privateLogger { get; set; }

    public Dictionary<string, ILogger> GetLoggers()
    {
        var dict = new Dictionary<string, ILogger>();
        dict[nameof(PublicLogger)] = PublicLogger;
        dict[nameof(_privateLogger)] = _privateLogger;
        
        return dict;
    }
}