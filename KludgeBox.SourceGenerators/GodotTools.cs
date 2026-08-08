using System;
using System.IO;
using System.Linq;

namespace KludgeBox.SourceGenerators;

internal static class GodotTools
{
    public const string GodotProjectFileName = "project.godot";
    
    public static string NormalizePath(string path)
    {
        if (String.IsNullOrWhiteSpace(path))
            return path;

        // Keep Godot-style resource paths unchanged
        if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
            return path;

        // If already absolute (e.g., C:/ or / on Unix), return as is
        if (Path.IsPathRooted(path))
            return path;

        // Normalize relative paths like ./ or ../
        return Path.GetFullPath(path);
    }
}