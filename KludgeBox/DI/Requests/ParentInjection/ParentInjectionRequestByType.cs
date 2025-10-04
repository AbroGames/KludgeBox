using Godot;
using KludgeBox.DI.Exceptions;
using KludgeBox.Reflection.Access;

namespace KludgeBox.DI.Requests.ParentInjection;

public class ParentInjectionRequestByType : IProcessingRequest
{
    private readonly IMemberAccessor _memberAccessor;
    private readonly Type _type;
    private readonly bool _deepSearch;

    public ParentInjectionRequestByType(IMemberAccessor memberAccessor, bool deepSearch)
    {
        if (!memberAccessor.Member.ReflectedType.IsAssignableTo(typeof(Node)))
        {
            throw new TargetIsNotANodeException($"The type {memberAccessor.Member.ReflectedType.FullName} is not a Node subtype");
        }
        
        _memberAccessor = memberAccessor;
        _deepSearch = deepSearch;
        _type = memberAccessor.ValueType;
    }
    
    public void ProcessOnInstance(object instance)
    {
        if (instance is not Node node)
            throw new TargetIsNotANodeException($"The type {instance.GetType().FullName} is not a Node subtype");

        
        Node foundNode = null;

        if (_deepSearch)
        {
            foundNode = GetParentByTypeDeep(node, _type);
        }
        else
        {
            var parent = node.GetParent();
            if (parent.GetType().IsAssignableTo(_type))
            {
                foundNode = parent;
            }
        }
        
        if (foundNode is null)
        {
            throw new NotFoundException($"Unable to find parent by type {_type.FullName} for node @ {node.GetPath()} (type of {node.GetType().FullName}). Target member: {_memberAccessor.Member.Name}");
        }
        
        _memberAccessor.SetValue(node, foundNode);
    }

    private Node GetParentByTypeDeep(Node node, Type type)
    {
        var parent = node.GetParent();
        if (parent is null)
        {
            return null;
        }

        if (parent.GetType().IsAssignableTo(type))
        {
            return parent;
        }
        
        return GetParentByTypeDeep(parent, type);
    }
}