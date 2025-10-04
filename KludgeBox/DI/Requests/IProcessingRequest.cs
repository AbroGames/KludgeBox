namespace KludgeBox.DI.Requests;


public interface IProcessingRequest
{
    void ProcessOnInstance(object instance);
}