using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.Logging;
using KludgeBox.Reflection.Access;
using Serilog;

namespace KludgeBox.DI.Requests.NotNullCheck;

public class NotNullCheckRequest : IProcessingRequest
{
    private readonly IMemberAccessor _memberAccessor;
    private bool _throwOnFail;
    
    [Logger] private ILogger _log;
    
    public NotNullCheckRequest(IMemberAccessor memberAccessor, bool throwOnFail)
    {
        Di.Process(this);
        
        _memberAccessor = memberAccessor;
        _throwOnFail = throwOnFail;
    }
    
    public void ProcessOnInstance(object instance)
    {
        var storedValue = _memberAccessor.GetValue(instance);
        if (storedValue is null)
        {
            var nodePathInfo = "";
            if (instance is Node node)
            {
                nodePathInfo = $" Target node is at {node.GetPath()}";
            }

            var exception = new NotNullCheckFailedException(
                $"Value stored in {_memberAccessor.Member.ReflectedType?.FullName}.{_memberAccessor.Member.Name} is null.{nodePathInfo}");
            _log.Error(exception,"Not null check failed for {type}.{member}. {nodePathInfo}",
                _memberAccessor.Member.ReflectedType?.FullName,
                _memberAccessor.Member.Name,
                nodePathInfo);

            if (_throwOnFail)
            {
                throw exception;
            }
        }
    }
}