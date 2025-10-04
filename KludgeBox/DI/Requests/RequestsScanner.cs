using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.DependencyCreation;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.DI.Requests.NotNullCheck;
using KludgeBox.DI.Requests.ParentInjection;
using KludgeBox.Reflection.Access;

namespace KludgeBox.DI.Requests;

public class RequestsScanner
{
    private List<IProcessingRequestScanner> _requestScanners;

    public RequestsScanner()
    {
        _requestScanners = new List<IProcessingRequestScanner>();
    }

    public static RequestsScanner CreateDefault()
    {
        var scanner = new RequestsScanner();
        scanner.RegisterRequestScanner(new LoggerInjectionRequestScanner());
        scanner.RegisterRequestScanner(new DependencyCreationRequestScanner());
        scanner.RegisterRequestScanner(new ChildInjectionRequestScanner());
        scanner.RegisterRequestScanner(new ParentInjectionRequestScanner());
        scanner.RegisterRequestScanner(new NotNullCheckRequestScanner());
        
        return scanner;
    }
    public void RegisterRequestScanner(IProcessingRequestScanner requestScanner)
    {
        _requestScanners.Add(requestScanner);
    }

    public List<IProcessingRequest> ScanRequests(IReadOnlyCollection<IMemberAccessor> memberAccessors)
    {
        var allRequests = new List<IProcessingRequest>();

        foreach (var requestScanner in _requestScanners)
        {
            if (TryGetRequests(requestScanner, memberAccessors, out var requests))
            {
                allRequests.AddRange(requests!);
            }
        }
        
        return allRequests;
    }

    public bool TryGetRequests(IProcessingRequestScanner scanner, IReadOnlyCollection<IMemberAccessor> accessors, out List<IProcessingRequest> injectionRequest)
    {
        List<IProcessingRequest> requests = new ();
        bool hasRequests = false;

        foreach (var accessor in accessors)
        {
            if (scanner.TryGetRequest(accessor, out var request))
            {
                requests.Add(request);
                hasRequests = true;
            }
        }
        
        injectionRequest = requests;
        return hasRequests;
    }
}