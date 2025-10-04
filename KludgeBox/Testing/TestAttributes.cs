namespace KludgeBox.Testing;

/// <summary>
/// Attribute that should be used to mark test methods.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TestAttribute : Attribute;

/// <summary>
/// Allows running the same test multiple times with different cases.
/// One attribute per case.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class TestCaseAttribute : Attribute
{
    public object[] TestParams => _params();
    
    private Func<object[]> _params;
    public TestCaseAttribute(Func<object[]> testParams)
    {
        _params = testParams;
    }

    public TestCaseAttribute(params object[] testParams)
    {
        _params = () => testParams;
    }

    public TestCaseAttribute(object param)
    {
        _params = () => [param];
    }
}

/// <summary>
/// Tests (methods or whole classes) marked with this attribute will not be executed.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SkipAttribute : Attribute
{
    public string Reason { get; }

    public SkipAttribute(string reason)
    {
        Reason = reason;
    }

    public SkipAttribute()
    {
        Reason = "No reason specified";
    }
}