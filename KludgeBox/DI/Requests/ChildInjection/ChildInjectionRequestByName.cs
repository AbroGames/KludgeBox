using Godot;
using Humanizer;
using KludgeBox.DI.Exceptions;
using KludgeBox.Reflection.Access;

namespace KludgeBox.DI.Requests.ChildInjection;

public class ChildInjectionRequestByName : IProcessingRequest
{
    private readonly IMemberAccessor _memberAccessor;
    private readonly string _name;
    private readonly bool _deepSearch;
    

    public ChildInjectionRequestByName(IMemberAccessor memberAccessor, string name, bool deepSearch)
    {
        if (!memberAccessor.Member.ReflectedType!.IsAssignableTo(typeof(Node)))
        {
            throw new TargetIsNotANodeException($"The type {memberAccessor.Member.ReflectedType.FullName} is not a Node subtype");
        }
        
        _memberAccessor = memberAccessor;
        _name = name;
        _deepSearch = deepSearch;
    }

    public ChildInjectionRequestByName(IMemberAccessor memberAccessor, string name) : this(memberAccessor, name, true) { }
    public ChildInjectionRequestByName(IMemberAccessor memberAccessor)              : this(memberAccessor, memberAccessor.Member.Name.Pascalize()) { }

    public void ProcessOnInstance(object instance)
    {
        if(instance is not Node node)
            throw new TargetIsNotANodeException($"The type {instance.GetType().FullName} is not a Node subtype");

        Node foundNode = null;
        foundNode = node.FindChild(_name, recursive: _deepSearch, owned: false);

        if (foundNode is null)
        {
            throw new NotFoundException($"Unable to {(_deepSearch ? "deep" : "shallow")} find children by name {_name} for node @ {node.GetPath()} (type of {node.GetType().FullName}). Target member: {_memberAccessor.Member.Name}");
        }
        
        _memberAccessor.SetValue(instance, foundNode);
    }
}