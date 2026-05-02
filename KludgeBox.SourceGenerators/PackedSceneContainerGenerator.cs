using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KludgeBox.SourceGenerators;

[Generator]
public class PackedSceneContainerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var attributeProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "KludgeBox.SourceGenerators.PackedSceneContainerAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => ExtractClassInfo(ctx, ct))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);
    }
    
    private static ClassInfo? ExtractClassInfo(
        GeneratorAttributeSyntaxContext ctx, 
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        if (ctx.TargetSymbol is not INamedTypeSymbol classSymbol)
            return null;

        var attribute = ctx.Attributes[0];
        var include = GetAttributeArray(attribute, "Include");
        var exclude = GetAttributeArray(attribute, "Exclude");
        var basePath = GetAttributeString(attribute, "BasePath") ?? "res://";

        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace 
            ? string.Empty 
            : classSymbol.ContainingNamespace.ToDisplayString();

        return new ClassInfo(
            classSymbol.Name,
            ns,
            include,
            exclude,
            basePath);
    }
    
    private static string[] GetAttributeArray(AttributeData attr, string name) =>
        attr.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == name)
            .Value.Values
            .Select(v => v.Value?.ToString() ?? "")
            .ToArray();

    private static string? GetAttributeString(AttributeData attr, string name) =>
        attr.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == name)
            .Value.Value?.ToString();
}