using System;
using System.IO;
using System.Linq;

namespace KludgeBox.SourceGenerators;

internal static class GodotTools
{
    public const string GodotProjectFileName = "project.godot";
    
    
    public static bool TryGetGodotRoot(string pathFrom, out string rootPath)
    {
        if (pathFrom.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Godot root path should not start with 'res://', but {pathFrom} was provided.");
        }
        
        var fullPath = Path.GetFullPath(pathFrom);
        rootPath = SearchForRoot(fullPath);

        if (rootPath is null)
        {
            return false;
        }
        
        return true;
        
        string SearchForRoot(string path)
        {
            while (true)
            {
                var hasProjectFile = Directory.EnumerateFiles(path)
                    .Select(Path.GetFileName)
                    .Contains(GodotProjectFileName);
                if (hasProjectFile)
                {
                    return path;
                }
                else
                {
                    path = Path.GetDirectoryName(path);
                    if (String.IsNullOrEmpty(path)) return null;
                }
            }
        }
    }
    
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