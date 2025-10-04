using Godot;

namespace KludgeBox.Testing;

/// <summary>
/// Test node. All test classes must inherit from this. To create test methods, make a public void SomeTestName() method and mark it with the <see cref="TestAttribute"/>.
/// </summary>
public abstract partial class TestNode : Node
{
    private TestContext _context;
    
    /// <summary>
    /// Human-readable name of the test
    /// </summary>
    public virtual string TestName => GetType().FullName;
    
    /// <summary>
    /// Test context for this entire class
    /// </summary>
    public TestContext Context => GetOrCreateContext();

    /// <summary>
    /// Sets up test state before running. Called once, not before every test. See <see cref="RunTests"/>
    /// </summary>
    public virtual void Setup() { }
    
    /// <summary>
    /// Performs some work after tests. Called once, not after every test. See <see cref="RunTests"/>
    /// </summary>
    public virtual void Teardown() { }

    public void RunTests()
    {
        Setup();
        Context.Run();
        Teardown();
    }

    private TestContext GetOrCreateContext()
    {
        if (_context is null)
            _context = TestsScanner.ScanTestMethods(this);

        return _context;
    }
}