namespace KludgeBox.Testing.Asserting;

public abstract class AssertException : Exception
{
    public AssertException(string message) : base(message) { }
    public AssertException(string message, Exception inner) : base(message, inner) { }
}

public class AssertFailException : AssertException
{
    public AssertFailException(string message) : base(message)
    {
    }

    public AssertFailException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class AssertResultException : AssertException
{
    public TestResult Result { get; }

    public AssertResultException(TestResult result, string message) : base(message)
    {
        Result = result;
    }
}

public class AssertSuccessException : AssertResultException
{
    public AssertSuccessException(string message) : base(TestResult.Passed, message)
    {
    }
}

public class AssertSkipException : AssertResultException
{
    public AssertSkipException(string message) : base(TestResult.Skipped, message) { }
}