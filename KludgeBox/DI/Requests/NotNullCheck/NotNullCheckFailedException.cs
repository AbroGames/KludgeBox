namespace KludgeBox.DI.Requests.NotNullCheck;

public class NotNullCheckFailedException : Exception
{
    public NotNullCheckFailedException(string message) : base(message){}
}