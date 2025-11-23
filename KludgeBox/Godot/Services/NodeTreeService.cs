using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace KludgeBox.Godot.Services;

public class NodeTreeService
{
    
    [Logger] ILogger _log;

    public NodeTreeService()
    {
        Di.Process(this);
    }
    
    /// <summary>
    /// Get full path for all children of this node.
    /// </summary>
    public string GetFullTree(Node node)
    {
        StringBuilder sb = new();
        sb.AppendLine(); 
        sb.Append(node.GetPath());
        foreach (var child in node.GetChildren()) 
        {
            sb.Append(GetFullTree(child));
        }
        return sb.ToString();
    }
    
    public void LogFullTree(Node node) => _log.Information(GetFullTree(node));
    
    /// <summary>
    /// Get hash of all children of this node.<br/>
    /// Can be used for check equality of Client/Server trees.
    /// </summary>
    public string GetTreeHash(Node node)
    {
        string inputString = GetFullTree(node);
        byte[] inputBytes = Encoding.UTF8.GetBytes(inputString);
        byte[] hashBytes = MD5.HashData(inputBytes);

        StringBuilder sb = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
    
    public void LogTreeHash(Node node) => _log.Information(GetTreeHash(node));
    
    /// <summary>
    /// Get data from each field and property with attribute <see cref="ExportAttribute"/>.
    /// </summary>
    public string GetExportMembersInfo(Node node)
    {
        StringBuilder sb = new StringBuilder();
        Type type = node.GetType();
        
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var members = type.GetMembers(flags);

        foreach (var member in members)
        {
            if (!Attribute.IsDefined(member, typeof(ExportAttribute))) continue;

            object value = member switch
            {
                FieldInfo field => field.GetValue(node),
                PropertyInfo prop => prop.GetValue(node),
                _ => null
            };

            sb.AppendLine($"{member.Name}: {value ?? "null"}");
        }

        return sb.ToString();
    }
    
    public void LogExportMembersInfo(Node node) => _log.Information(GetExportMembersInfo(node));
}