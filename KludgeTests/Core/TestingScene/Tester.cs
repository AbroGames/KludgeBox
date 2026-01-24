using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace KludgeBox.Testing.TestingScene;

public partial class Tester : Node
{
    private List<TestContext> _rootContexts = new();
    private HashSet<TestContext> _finalContexts = new ();
    
    [Export] private Node _trackersContainer;
    [Export] private Label _totalLabel;
    [Export] private Label _passedLabel;
    [Export] private Label _failedLabel;
    [Export] private Label _skippedLabel;
    [Export] private Label _unknownLabel;
    
    
    private static int _checkInterval = 5;
    private int _ticksToCheck = _checkInterval;
    
    public override void _Ready()
    {
        GD.Print($"Scanning tests");
        var testNodes = TestsScanner.ScanTestClasses();
        //List<TestContext> rootContexts = new();
        Dictionary<TestContext, List<TestNode>> rootedTestNodes = new();
        foreach (var testNode in testNodes)
        {
            var ctxAttribute = testNode.GetType().GetCustomAttribute<TestGroupAttribute>();
            string rootCtxName;
            if (ctxAttribute is not null)
            {
                rootCtxName = ctxAttribute.ContextName;
            }
            else
            {
                rootCtxName = "No context";
            }
            
            var rootCtx = _rootContexts.FirstOrDefault(ctx => ctx.Name == rootCtxName);
            if (rootCtx is null)
            {
                rootCtx = new TestContext(rootCtxName, TestResult.NotRan);
                rootedTestNodes[rootCtx] = new();
                _rootContexts.Add(rootCtx);
            }
            rootCtx.AddChild(testNode.Context);
            rootedTestNodes[rootCtx].Add(testNode);
        }
        
        
        GD.Print($"Found {testNodes.Count} test instances.");

        foreach (var rootContext in _rootContexts)
        {
            var rootTracker = new TestTracker(rootContext);
            _trackersContainer.AddChild(rootTracker);

            foreach (var testNode in rootedTestNodes[rootContext])
            {
                var tracker = new TestTracker(testNode.Context);
                tracker.AddChild(testNode);
                _trackersContainer.AddChild(tracker);
            
                ScanContexts(testNode.Context);
            }
        }
        
        Callable.From(RunTests).CallDeferred();
    }

    private void ScanContexts(TestContext testContext)
    {
        if (testContext.Children.Count == 0)
            return;
            
        foreach (var childContext in testContext.Children)
        {
            var tracker = new TestTracker(childContext);
            _trackersContainer.AddChild(tracker);
        }
    }

    private void RunTests()
    {
        foreach (var rootContext in _rootContexts)
        {
            rootContext.Run(throwOnOtherExceptions: false);
            AggregateFinalContexts(rootContext);
        }
    }
    
    private void AggregateFinalContexts(TestContext root)
    {
        var result = new List<TestContext>();
        if (root.Children.Count == 0)
        {
            return;
        }
        
        foreach (var childContext in root.Children)
        {
            if (childContext.Children.Count == 0)
            {
                _finalContexts.Add(childContext);
            }
            else
            {
                AggregateFinalContexts(childContext);
            }
        }
    }
    
    public override void _Process(double delta)
    {
        _ticksToCheck--;

        if (_ticksToCheck <= 0)
        {
            UpdateUi();
            _ticksToCheck = _checkInterval;
        }
    }

    private void UpdateUi()
    {
        int totalTests = _finalContexts.Count;
        int passedTests = _finalContexts.Count(ctx => ctx.Result == TestResult.Passed);
        int failedTests = _finalContexts.Count(ctx => ctx.Result == TestResult.Failed);
        int skippedTests = _finalContexts.Count(ctx => ctx.Result == TestResult.Skipped);
        int unknownTests = _finalContexts.Count(ctx => ctx.Result == TestResult.Unknown);
        
        _totalLabel.Text = $"Finished ({(passedTests + failedTests + skippedTests)}/{totalTests} tests))";
        _passedLabel.Text = $"Passed ({passedTests} tests)";
        _failedLabel.Text = $"Failed ({failedTests} tests)";
        _skippedLabel.Text = $"Skipped ({skippedTests} tests)";
        _unknownLabel.Text = $"Unknown ({unknownTests} tests)";
    }
}