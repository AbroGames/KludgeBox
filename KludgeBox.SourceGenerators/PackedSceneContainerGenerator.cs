using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KludgeBox.SourceGenerators;

[Generator]
public class PackedSceneContainerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0,
                transform: static (ctx, _) => GetTarget(ctx))
            .Where(static m => m is not null);

        context.RegisterSourceOutput(classes, static (spc, data) =>
        {
            Generate(spc, data!);
        });
    }
    
    private static ClassData? GetTarget(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol symbol)
            return null;

        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "PackedSceneContainerAttribute")
            {
                var include = GetArray(attr, 0);
                var exclude = GetArray(attr, 1);

                return new ClassData(symbol, include, exclude);
            }
        }

        return null;
    }
    
    
    private static string[] GetArray(AttributeData attr, int index)
    {
        if (attr.ConstructorArguments.Length <= index)
            return Array.Empty<string>();

        var arg = attr.ConstructorArguments[index];

        if (arg.Kind == TypedConstantKind.Array)
        {
            return arg.Values
                .Select(v => v.Value?.ToString() ?? "")
                .ToArray();
        }

        return Array.Empty<string>();
    }

    private static void Generate(SourceProductionContext context, ClassData data)
    {
        var projectRoot = FindProjectRoot();

        if (projectRoot == null)
            return;

        var scenes = ScanScenes(projectRoot, data.Include, data.Exclude);

        var source = GenerateCode(data, scenes);

        context.AddSource($"{data.ClassSymbol.Name}.PackedScenes.g.cs", source);
    }

    private static string? FindProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();

        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "project.godot")))
                return dir;

            dir = Directory.GetParent(dir)?.FullName;
        }

        return null;
    }
    
     private static List<string> ScanScenes(string root, string[] include, string[] exclude)
    {
        var all = Directory.GetFiles(root, "*.tscn", SearchOption.AllDirectories)
            .Select(p => ToGodotPath(root, p))
            .ToList();

        bool Match(string path, string rule)
        {
            rule = Normalize(rule);
            path = Normalize(path);

            return path.Contains(rule);
        }

        var result = all
            .Where(p =>
                (include.Length == 0 || include.Any(i => Match(p, i))) &&
                !exclude.Any(e => Match(p, e)))
            .ToList();

        return result;
    }

    private static string Normalize(string path)
    {
        path = path.Replace("\\", "/");

        if (path.StartsWith("res://"))
            path = path.Substring(6);

        if (path.StartsWith("/"))
            path = path.Substring(1);

        return path;
    }

    private static string ToGodotPath(string root, string fullPath)
    {
        var rel = GetRelativePath(root, fullPath).Replace("\\", "/");
        return $"res://{rel}";
    }

    private static string GenerateCode(ClassData data, List<string> scenes)
    {
        var ns = data.ClassSymbol.ContainingNamespace.ToDisplayString();
        var className = data.ClassSymbol.Name;

        var sb = new StringBuilder();

        sb.AppendLine("using Godot;");
        sb.AppendLine();

        sb.AppendLine($"namespace {ns}");
        sb.AppendLine("{");
        sb.AppendLine($"    public partial class {className}");
        sb.AppendLine("    {");

        foreach (var scene in scenes)
        {
            var prop = Path.GetFileNameWithoutExtension(scene);
            sb.AppendLine($"        /// <summary>{scene}</summary>");
            sb.AppendLine($"        public PackedScene {prop} {{ get; }}");
        }

        sb.AppendLine();
        sb.AppendLine($"        public {className}()");
        sb.AppendLine("        {");

        foreach (var scene in scenes)
        {
            var prop = Path.GetFileNameWithoutExtension(scene);
            sb.AppendLine($"            {prop} = GD.Load<PackedScene>(\"{scene}\");");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private record ClassData
    {
        public ClassData(INamedTypeSymbol classSymbol,
            string[] include,
            string[] exclude)
        {
            ClassSymbol = classSymbol;
            Include = include;
            Exclude = exclude;
        }

        public INamedTypeSymbol ClassSymbol { get; }
        public string[] Include { get; }
        public string[] Exclude { get; }

        public void Deconstruct(out INamedTypeSymbol classSymbol, out string[] include, out string[] exclude)
        {
            classSymbol = ClassSymbol;
            include = Include;
            exclude = Exclude;
        }
    }
    
    private static string GetRelativePath(string relativeTo, string path)
    {
        var fromUri = new Uri(AppendDirectorySeparator(relativeTo));
        var toUri = new Uri(path);

        if (fromUri.Scheme != toUri.Scheme)
            return path; // fallback

        var relativeUri = fromUri.MakeRelativeUri(toUri);
        var result = Uri.UnescapeDataString(relativeUri.ToString());

        return result.Replace('/', Path.DirectorySeparatorChar);
    }

    private static string AppendDirectorySeparator(string path)
    {
        if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            return path + Path.DirectorySeparatorChar;

        return path;
    }
}