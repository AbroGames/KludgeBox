using KludgeBox.Logging;
using KludgeBox.Reflection.Access;

namespace KludgeBox.DI.Requests.LoggerInjection;

public class LoggerInjectionRequest : IProcessingRequest
{
    
    private readonly IMemberAccessor _memberAccessor;

    public LoggerInjectionRequest(IMemberAccessor memberAccessor)
    {
        _memberAccessor = memberAccessor;
    }

    public void ProcessOnInstance(object instance)
    {
        var logger = LogFactory.GetForStatic(_memberAccessor.Member.DeclaringType);
        _memberAccessor.SetValue(instance, logger);
    }
}