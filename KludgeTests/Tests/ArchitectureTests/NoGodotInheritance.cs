using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Godot;
using KludgeBox.Testing;
using KludgeBox.Testing.Asserting;

namespace KludgeTests.Tests.ArchitectureTests;

/// <summary>
/// Verifies that the KludgeBox assembly does not drag Godot types into the
/// inheritance hierarchy of its own types.
/// </summary>
[TestGroup("Architecture Tests")]
public partial class NoGodotInheritance : TestNode
{
    private static readonly HashSet<string> GodotAssemblyNames = new()
    {
        "GodotSharp",
        "GodotSharpEditor"
    };

    /// <summary>
    /// Assemblies that may legitimately pull GodotSharp into their dependency graph
    /// without it counting as an inheritance violation. The tests are only concerned
    /// with the KludgeBox library itself.
    /// </summary>
    private const string KludgeBoxAssemblyName = "KludgeBox";

    public override string TestName => "No Godot types in inheritance hierarchy";

    [Test]
    public void NoGodotNamespaceInHierarchy()
    {
        var assembly = GetKludgeBoxAssembly();
        var violations = CollectViolations(assembly, IsGodotNamespaceType);

        PrintViolations("namespace (Godot*)", violations);

        Assert.AreEqual(0, violations.Count,
            BuildFailMessage("Godot-namespace types found in inheritance hierarchy", violations));
    }

    [Test]
    public void NoGodotAssemblyInHierarchy()
    {
        var assembly = GetKludgeBoxAssembly();
        var violations = CollectViolations(assembly, IsGodotAssemblyType);

        PrintViolations("assembly (GodotSharp / GodotSharpEditor)", violations);

        Assert.AreEqual(0, violations.Count,
            BuildFailMessage("Types from GodotSharp/GodotSharpEditor found in inheritance hierarchy", violations));
    }

    private static Assembly GetKludgeBoxAssembly()
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == KludgeBoxAssemblyName);
        Assert.IsNotNull(assembly,
            $"{KludgeBoxAssemblyName} assembly is not loaded into the current AppDomain");
        return assembly!;
    }

    /// <summary>
    /// Walks the base-type chain of every KludgeBox type and records every ancestor
    /// that matches <paramref name="isViolation"/>.
    /// </summary>
    private static Dictionary<Type, List<Type>> CollectViolations(
        Assembly assembly, Func<Type, bool> isViolation)
    {
        var violations = new Dictionary<Type, List<Type>>();

        foreach (var type in assembly.GetTypes())
        {
            var current = type.BaseType;
            while (current is not null)
            {
                if (isViolation(current))
                    AddAncestor(violations, type, current);

                current = current.BaseType;
            }
        }

        return violations;
    }

    private static bool IsGodotNamespaceType(Type type)
        => type.Namespace is string ns
           && (ns == "Godot" || ns.StartsWith("Godot.", StringComparison.Ordinal));

    private static bool IsGodotAssemblyType(Type type)
        => GodotAssemblyNames.Contains(type.Assembly.GetName().Name);

    private static void AddAncestor(Dictionary<Type, List<Type>> target, Type type, Type ancestor)
    {
        if (!target.TryGetValue(type, out var list))
        {
            list = new List<Type>();
            target[type] = list;
        }

        if (!list.Contains(ancestor))
            list.Add(ancestor);
    }

    private static void PrintViolations(string category, Dictionary<Type, List<Type>> violations)
    {
        GD.Print($"[Architecture] Checking inheritance by {category}: " +
                 $"{violations.Count} type(s) with violations");

        foreach (var (type, ancestors) in violations)
        {
            GD.Print($"  [FAIL] {type.FullName} : {string.Join(" -> ", ancestors.Select(a => a.FullName))}");
        }
    }

    private static string BuildFailMessage(string title, Dictionary<Type, List<Type>> violations)
    {
        var sb = new StringBuilder();
        sb.Append(title).Append($" ({violations.Count} type(s)):");
        foreach (var (type, ancestors) in violations)
        {
            sb.Append("\n  ").Append(type.FullName).Append(" : ")
              .Append(string.Join(" -> ", ancestors.Select(a => a.FullName)));
        }

        return sb.ToString();
    }
}
