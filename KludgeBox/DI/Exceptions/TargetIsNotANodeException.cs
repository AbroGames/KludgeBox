namespace KludgeBox.DI.Exceptions;

public class TargetIsNotANodeException : Exception
{
    public TargetIsNotANodeException()
    {
    }

    public TargetIsNotANodeException(string message) : base(message)
    {
    }
}