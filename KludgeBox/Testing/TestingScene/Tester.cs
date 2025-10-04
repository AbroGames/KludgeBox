using Godot;

namespace KludgeBox.Testing.TestingScene;

public partial class Tester : Node
{
    private List<TestTracker> _trackers = new();
    private List<TestNode> _rootTestNodes = new();
    [Export] private Node _trackersContainer;
    [Export] private Node _totalLabel;
    [Export] private Node _passedLabel;
    [Export] private Node _failedLabel;
    [Export] private Node _skippedLabel;
    [Export] private Node _unknownLabel;
    
    
    private static int _checkInterval = 5;
    private int _ticksToCheck = _checkInterval;
    
    public override void _Ready()
    {
        GD.Print($"Scanning tests");
        var testNodes = TestsScanner.ScanTestClasses();
        GD.Print($"Found {testNodes.Count} test instances.");
        
        foreach (var testNode in testNodes)
        {
            var tracker = new TestTracker(testNode.Context);
            _trackers.Add(tracker);
            tracker.AddChild(testNode);
            _rootTestNodes.Add(testNode);
            _trackersContainer.AddChild(tracker);
            
            ScanContexts(testNode.Context);
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
            _trackers.Add(tracker);
            _trackersContainer.AddChild(tracker);
        }
    }

    private void RunTests()
    {
        foreach (var testNode in _rootTestNodes)
        {
            testNode.RunTests();
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
        
    }
}