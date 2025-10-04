using KludgeBox.Reflection.Access;

namespace KludgeBox.DI.Requests.LoggerInjection;

public class LoggerInjectionRequestScanner : IProcessingRequestScanner
{
    public bool TryGetRequest(IMemberAccessor accessor, out IProcessingRequest injectionRequest)
    {
        if (!accessor.TryGetAttribute<LoggerAttribute>(out _))
        {
            injectionRequest = null;
            return false;
        }

        injectionRequest = new LoggerInjectionRequest(accessor);
        return true;
    }
}