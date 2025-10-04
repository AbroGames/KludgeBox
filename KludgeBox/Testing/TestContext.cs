using System.Reflection;
using KludgeBox.Testing.Asserting;

namespace KludgeBox.Testing;

public enum TestResult
{
    NotRan,
    Unknown,
    Passed,
    Skipped,
    Failed,
    Errored
}

/// <summary>
/// The test context. Each test class gets one, and each individual test and test case gets its own separate context as well.
/// </summary>
public class TestContext
{
    public string Name { get; }
    public string ResultMessage { get; private set; }
    public TestContext Parent { get; private set; }

    public IReadOnlyList<TestContext> Children => _children;
    public TestResult Result { get; private set; }
    public bool MustBeSkipped => _initialState == TestResult.Skipped;
    public string SkipReason { get; internal set; } = "Not skipped";
    

    private List<TestContext> _children = new();
    private Action _action;
    private TestResult _initialState;

    public TestContext(string testName, TestResult initialState)
    {
        Name = testName ?? throw new ArgumentNullException(nameof(testName));
        _initialState = initialState;
        Result = _initialState;
    }

    public void AddChild(TestContext child)
    {
        if (Result is not TestResult.NotRan)
            throw new InvalidOperationException($"Attempt to add child test context '{child.Name}' to already executed parent test context '{Parent.GetFullName()}'.");
        
        _children.Add(child);
        child.AttachTo(this);
    }

    public void BindAction(Action action)
    {
        _action = action;
    }

    private void AttachTo(TestContext parent)
    {
        if (Parent is not null)
            throw new InvalidOperationException($"Attempt to reattach child test '{Name}' from '{Parent.GetFullName()}' to '{parent.GetFullName()}");
        
        Parent = parent;
    }

    public void Reset()
    {
        Result = _initialState;
        foreach (var childContext in _children)
        {
            childContext.Reset();
        }
    }

    public TestResult Run(bool throwOnFail = false, bool throwOnOtherExceptions = true, bool forceSkip = false, Action callback = null)
    {
        try
        {
            if (!forceSkip && !MustBeSkipped)
            {
                _action?.Invoke();
                Result = TestResult.Passed;
            }
            else
            {
                if (forceSkip)
                {
                    SkipReason = "Skipped by parent test";
                }
                ResultMessage = SkipReason;
            }
        }
        catch (Exception e)
        {
            if (e is TargetInvocationException && e.InnerException is not null)
            {
                e = e.InnerException;
            }
            
            ResultMessage = e.Message;
            if (e is AssertException)
            {
                if (e is AssertFailException failException)
                {
                    Result = TestResult.Failed;
                    if (throwOnFail)
                        throw;
                }
                
                if (e is AssertResultException resultException)
                {
                    Result = resultException.Result;
                    ResultMessage = resultException.Message;
                }
            }
            else
            {
                Result = TestResult.Errored;
                if (throwOnOtherExceptions)
                    throw;
            }
        }

        foreach (var childContext in _children)
        {
            Result = Combine(Result, childContext.Run(throwOnFail, throwOnOtherExceptions, forceSkip || MustBeSkipped));
        }
        
        return Result;
    }

    public string GetFullName()
    {
        if (Parent is null)
        {
            return Name;
        }
        
        return Path.Combine(Parent.GetFullName(), Name);
    }

    public int GetDepth()
    {
        if (Parent is null)
            return 0;
        
        return Parent.GetDepth() + 1;
    }

    public static TestResult Combine(TestResult current, TestResult next)
    {
        if (next > current)
        {
            return next;
        }
        
        return current;
    }
}