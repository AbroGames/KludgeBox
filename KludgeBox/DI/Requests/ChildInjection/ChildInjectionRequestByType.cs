using Godot;
using Godot.Collections;
using KludgeBox.DI.Exceptions;
using KludgeBox.Reflection.Access;

namespace KludgeBox.DI.Requests.ChildInjection;

public class ChildInjectionRequestByType : IProcessingRequest
{
    private readonly IMemberAccessor _memberAccessor;
    private readonly Type _type;
    private readonly bool _deepSearch;
    

    public ChildInjectionRequestByType(IMemberAccessor memberAccessor, Type type, bool deepSearch)
    {
        if (!memberAccessor.Member.ReflectedType.IsAssignableTo(typeof(Node)))
        {
            throw new TargetIsNotANodeException($"The type {memberAccessor.Member.ReflectedType.FullName} is not a Node subtype");
        }
        
        _memberAccessor = memberAccessor;
        _type = type;
        _deepSearch = deepSearch;
    }
    
    public ChildInjectionRequestByType(IMemberAccessor memberAccessor, bool deepSearch) : this(memberAccessor, memberAccessor.ValueType, deepSearch) { }

    public ChildInjectionRequestByType(IMemberAccessor memberAccessor, Type type) : this(memberAccessor, type, true) { }
    public ChildInjectionRequestByType(IMemberAccessor memberAccessor)              : this(memberAccessor, memberAccessor.ValueType) { }

    public void ProcessOnInstance(object instance)
    {
        if(instance is not Node node)
            throw new TargetIsNotANodeException($"The type {instance.GetType().FullName} is not a Node subtype");

        Node foundNode = null;
        if (!_deepSearch)
        {
            foundNode = FindByTypeShallow(_type, node);
        }
        else
        {
            foundNode = FindByTypeDeep(_type, node);
        }

        if (foundNode is null)
        {
            throw new NotFoundException($"Unable to {(_deepSearch ? "deep" : "shallow")} find children by type {_type.FullName} for node @ {node.GetPath()} (type of {node.GetType().FullName}). Target member: {_memberAccessor.Member.Name}");
        }
        
        _memberAccessor.SetValue(instance, foundNode);
    }

    private Node FindByTypeDeep(Type type, Node root)
    {
        Array<Node> children = root.GetChildren();
        var found = children.FirstOrDefault(c => c.GetType().IsAssignableTo(type));

        if (found is null)
        {
            foreach (Node child in children)
            {
                found = FindByTypeDeep(type, child);
                if (found is not null)
                    break;
            }
        }
        
        return found;
    }


    private Node FindByTypeShallow(Type type, Node root)
    {
        Array<Node> children = root.GetChildren();

        return children.FirstOrDefault(c => c.GetType().IsAssignableTo(type));
    }
}