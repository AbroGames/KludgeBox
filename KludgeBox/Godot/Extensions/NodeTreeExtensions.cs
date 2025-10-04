using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Godot;
using Godot.Collections;

namespace KludgeBox.Godot.Extensions;

public static class NodeTreeExtensions
{
    
    private static ulong _nodeCounter;
    
    /// <summary>
    /// Shortcut for GodotObject.IsInstanceValid(object)
    /// </summary>
    /// <param name="gdObj"></param>
    /// <returns></returns>
    public static bool IsValid(this GodotObject gdObj)
    {
        return GodotObject.IsInstanceValid(gdObj);
    }
    
    /// <summary>
    /// Rename the child node and add it to current node.
    /// </summary>
    public static void AddChildWithName(this Node node, Node child, string name)
    {
        child.SetName(name);
        node.AddChild(child);
    }
    
    /// <summary>
    /// Rename the child node to unique name with prefix and add it to current node.
    /// </summary>
    public static void AddChildWithUniqueName(this Node node, Node child, string prefix, bool dashAfterPrefix = true)
    {
        AddChildWithName(node, child, prefix + 
                                      (dashAfterPrefix ? "-" : "") + 
                                      node.GetMultiplayer().GetUniqueId() + 
                                      "-" + 
                                      _nodeCounter++);
    }
    
    /// <summary>
    /// Rename the child node to unique name and add it to current node.
    /// </summary>
    public static void AddChildWithUniqueName(this Node node, Node child)
    {
        AddChildWithUniqueName(node, child, "", false);
    }
    
    /// <summary>
    /// Extension method that reparents the given node to a new parent node.
    /// If the current parent node is valid, the node is reparented to the new parent node using Reparent().
    /// If the current parent node is invalid but the new parent node is valid, the node is added as a child to the new parent.
    /// </summary>
    /// <param name="node">The node to be reparented.</param>
    /// <param name="newParent">The new parent node to reparent to.</param>
    public static void ParentTo(this Node node, Node newParent)
    {
        var currentParent = node.GetParent();

        if (currentParent.IsValid())
        {
            node.Reparent(newParent, true);
            return;
        }

        if (newParent.IsValid())
        {
            newParent.AddChild(node);
            node.Owner = newParent;
        }
    }
    
    /// <summary>
    /// Sets the node as last in the queue for processing (rendering). As a result, it will be drawn on top of all other nodes.
    /// </summary>
    public static void ToForeground(this Node node)
    {
        var parent = node.GetParent();

        if (GodotObject.IsInstanceValid(parent))
        {
            parent.MoveChild(node, -1);
        }
    }

    /// <summary>
    /// Sets the node as first in the queue for processing (rendering). As a result, all other nodes will be drawn on top of it.
    /// </summary>
    public static void ToBackground(this Node node)
    {
        var parent = node.GetParent();

        if (GodotObject.IsInstanceValid(parent))
        {
            parent.MoveChild(node, 0);
        }
    }
    
    /// <summary>
    /// Returns the first child node of the specified type or null if there is no node of that type.
    /// </summary>
    /// <typeparam name="T">The type of the child node to retrieve.</typeparam>
    /// <param name="node">The parent node to search for the child node.</param>
    /// <returns>The first child node of the specified type, or null if no such node is found.</returns>
    public static T FindChild<T>(this Node node) where T : class
    {
        if (node.GetChildCount() < 1)
            return default;

        List<T> children = node.GetChildren<T>();
        return children.FirstOrDefault();
    }

    
    /// <summary>
    /// Retrieves or creates a child of the specified type and adds it as a child to the calling Node.
    /// If a child of the specified type already exists, it is returned; otherwise, a new instance is created and added.
    /// </summary>
    /// <typeparam name="T">The type of the child Node to retrieve or create.</typeparam>
    /// <param name="node">The Node to which the child will be added.</param>
    /// <remarks>
    /// This method is useful for ensuring that a specific type of child Node always exists as a direct child of the calling Node.
    /// If a child of the specified type is already present, it is returned; otherwise, a new instance is created, added, and returned.
    /// </remarks>
    public static T FindOrAddChild<T>(this Node node) where T : Node, new()
    {
        var found = node.FindChild<T>();
        if (found is not null) return found;

        var child = new T();
        node.AddChild(child);
        return child;
    }

    /// <summary>
    /// Returns a list of child nodes of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the child nodes to retrieve.</typeparam>
    /// <param name="node">The parent node to search for the child nodes.</param>
    /// <returns>A list containing all child nodes of the specified type.</returns>
    public static List<T> GetChildren<T>(this Node node) where T : class
    {
        if (node.GetChildCount() < 1)
            return new List<T>();
        
        Array<Node> children = node.GetChildren();
        return children.OfType<T>().ToList();
    }

    /// <summary>
    /// Attempts to find the nearest parent of the specified type. The type can also be an interface.<br/>
    /// Returns null if such a parent is not encountered up to the root.
    /// </summary>
    public static T GetParent<T>(this Node child) where T : class
    {
        var parent = child.GetParent();
        if (parent is null)
            return default;

        if (parent is T)
            return parent as T;

        return parent.GetParent<T>();
    }

    /// <summary>
    /// Этот метод может быть полезен, когда при удалении одного узла нужно добавить на его место новый. В этой ситуации
    /// может случиться так, что узел удаляется потому, что его родительский узел тоже удаляется. В этот момент попытка добавить на место узла другой
    /// вызовет ошибку. Этот метод подождет до конца кадра и только после этого ПОПЫТАЕТСЯ добавить узел.
    /// </summary>
    public static void TryAddChildDeferred(this Node parent, Node child, Action callback = null)
    {
        Callable.From(() =>
        {
            if (parent.IsValid())
            {
                parent.AddChild(child);
                callback?.Invoke();
            }
        }).CallDeferred();
    }
    
    /// <summary>
    /// Get full path for all children of this node
    /// </summary>
    public static string GetFullTree(this Node node)
    {
        StringBuilder sb = new();
        sb.AppendLine(); 
        sb.Append(node.GetPath());
        foreach (var child in node.GetChildren()) 
        {
            sb.Append(child.GetFullTree());
        }
        return sb.ToString();
    }
    
    /// <summary>
    /// Get hash of all children of this node
    /// Can be used for compare Client/Server trees in debug
    /// </summary>
    public static string GetTreeHash(this Node node)
    {
        string inputString = node.GetFullTree();
        byte[] inputBytes = Encoding.UTF8.GetBytes(inputString);
        byte[] hashBytes = MD5.HashData(inputBytes);

        StringBuilder sb = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
    
    /// <summary>
    /// Get data from each field and property with attribute [Export]
    /// </summary>
    public static string GetExportMembersInfo(this Node node)
    {
        StringBuilder stringBuilder = new();
        Type type = node.GetType();
        foreach (PropertyInfo property in type.GetProperties())
        {
            if (!Attribute.IsDefined(property, typeof(ExportAttribute))) continue;
            
            stringBuilder.AppendLine();
            stringBuilder.Append(property.Name + ": " + property.GetValue(node));
        }
        foreach (FieldInfo field in type.GetFields())
        {
            if (!Attribute.IsDefined(field, typeof(ExportAttribute))) continue;
            
            stringBuilder.AppendLine();
            stringBuilder.Append(field.Name + ": " + field.GetValue(node));
        }
        
        return stringBuilder.ToString();
    }
}