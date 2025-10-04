using KludgeBox.Reflection.Access;

namespace KludgeBox.DI.Requests;

public interface IProcessingRequestScanner
{
    bool TryGetRequest(IMemberAccessor accessor, out IProcessingRequest injectionRequest);
}