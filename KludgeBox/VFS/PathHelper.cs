using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace KludgeBox.VFS;

public static class PathHelper
{
    public static char DirectorySeparatorChar => '/';
    public static char AltDirectorySeparatorChar => '\\';
    
    public static Regex WindowsRootRegex { get; } = new Regex(@"^([a-zA-Z]:[/|\\])(.*)");
    public static Regex SchemeRegex { get; } = new Regex(@"^([a-zA-Z\d]*://)(.*)");
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDirectorySeparator(this char c)
    {
        return c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;
    }

    public static string EnsureEndingDirectorySeparator(this string path)
    {
        if (path.EndsWith(DirectorySeparatorChar))
            return path;
        return path + DirectorySeparatorChar;
    }
    
    /// <summary>
    /// Trims one trailing directory separator beyond the root of the path.
    /// </summary>
    public static string TrimEndingDirectorySeparator(this string path) =>
        EndsInDirectorySeparator(path) && !IsRoot(path) ?
            path!.Substring(0, path.Length - 1) :
            path;

    public static bool IsRoot(this string path)
    {
        // Handle null or empty paths
        if (String.IsNullOrEmpty(path))
            return false;
        
        // Check Unix-like root (/)
        if (path?.Length == 1 && path[0].IsDirectorySeparator())
            return true;
        
        // Check windows-like root (C:/, D:\, etc)
        if (Regex.IsMatch(path, @"^[a-zA-Z]:[/\\]$"))
            return true;
        
        // Check URI-like root (scheme://, http://, ftp://, etc)
        if(Regex.IsMatch(path,@"^[a-zA-Z\d]*://$"))
            return true;

        return false;
    }

    public static string GetRoot(this string path)
    {
        if(String.IsNullOrEmpty(path))
            return String.Empty;

        if (path.StartsWith("/"))
            return "/";

        if (WindowsRootRegex.IsMatch(path))
            return WindowsRootRegex.Match(path).Groups[1].ToString().ToUpper().Replace("\\", "/");
        
        if(SchemeRegex.IsMatch(path))
            return SchemeRegex.Match(path).Groups[1].ToString().ToLower();
        
        return String.Empty;
    }
    
    /// <summary>
    /// Returns true if the path ends in a directory separator.
    /// </summary>
    internal static bool EndsInDirectorySeparator(this string path) =>
        !string.IsNullOrEmpty(path) && IsDirectorySeparator(path[path.Length - 1]);
    
    public static string Combine(params string[] paths)
    {
        var normalizedPaths = new List<string>();
        foreach (var path in paths)
        {
            if(path is null)
                throw new ArgumentNullException();
            normalizedPaths.Add(NormalizePath(path));
        }

        string clrPath = Path.Combine(normalizedPaths.ToArray());
        return NormalizePath(clrPath);
    }

    public static string NormalizePath(this string path)
    {
        // Trim whitespaces
        string trimmedPath = path.Trim();
        
        // preserve trailing separator
        bool hasTrailingSeparator = Regex.IsMatch(trimmedPath, @"[/\\]$") && !IsRoot(trimmedPath);
        
        // Preserve scheme/root
        string root = GetRoot(trimmedPath);
        string pathComponent = trimmedPath.Substring(root.Length);
        
        // Remove redundant directory separators.
        // This also will break scheme if not preserved.
        pathComponent = Regex.Replace(pathComponent, @"(\\|\/){2,}", DirectorySeparatorChar.ToString());
        
        // Cleanup and recombine path.
        var parts = PathParts(pathComponent);
        var combinedPath = root + String.Join('/', parts) + (hasTrailingSeparator ? "/" : "");
        
        // Normalize separators.
        combinedPath = Regex.Replace(combinedPath, @"\\", DirectorySeparatorChar.ToString());
        
        return combinedPath;
    }

    public static bool HasRoot(this string path)
    {
        return !String.IsNullOrEmpty(GetRoot(path));
    }

    public static string RemoveRoot(this string path)
    {
        var root = GetRoot(path);
        if (String.IsNullOrEmpty(root))
            return path;

        return NormalizePath(path.Substring(root.Length));
    }

    public static string EnsureRoot(this string path)
    {
        if (!HasRoot(path))
        {
            return "/" + path;
        }

        return path;
    }
    
    public static bool HasExtension(this string path)
    {
        if (!String.IsNullOrWhiteSpace(path))
            return HasExtension(path.AsSpan());
        
        return false;
    }

    public static bool HasExtension(this ReadOnlySpan<char> path)
    {
        for (int i = path.Length - 1; i >= 0; i--)
        {
            char ch = path[i];
            if (ch == '.')
            {
                return i != path.Length - 1;
            }
            if (IsDirectorySeparator(ch))
                break;
        }
        return false;
    }

    public static string GetParent(this string path)
    {
        if(IsRoot(path))
            return path;
        
        var normalizedPath = NormalizePath(path);
        var trimmedPath = TrimEndingDirectorySeparator(normalizedPath);
        var parent = trimmedPath.Substring(0, trimmedPath.LastIndexOf(DirectorySeparatorChar) + 1);
        return parent;
    }

    public static string[] PathParts(this string path)
    {
        var parts = new List<string>();
        var separator = new Regex(@"[/\\]");
        var root = GetRoot(path);
        var pathComponent = path;
        
        if (root.Length >= 0)
        {
            pathComponent = path.Substring(root.Length);
        }
        var pathParts = separator.Split(pathComponent);

        parts.Add(root);
        parts.AddRange(pathParts.Select(part => part.Trim()));
        var result = parts.Where(part => !String.IsNullOrEmpty(part)).ToArray();
        return result;
    }

    public static string[] PathsChain(this string path) => InternalPathsChain(NormalizePath(path));
    private static string[] InternalPathsChain(string path, Queue<string> pathsStack = null)
    {
        pathsStack ??= new Queue<string>();
        if (path.LastIndexOf(DirectorySeparatorChar) != 0 && !IsRoot(path) && GetParent(path)!="")
            InternalPathsChain(GetParent(path), pathsStack);
        
        pathsStack.Enqueue(path);
        return pathsStack.ToArray();
    }
    
    public static string GetName(this string path)
    {
        if (String.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));
        
        if (IsRoot(path))
            return String.Empty;

        var trimmedPath = path.TrimEndingDirectorySeparator();
        
        if (!trimmedPath.Contains(DirectorySeparatorChar) && !trimmedPath.Contains(AltDirectorySeparatorChar))
            return trimmedPath;
        
        var normalizedPath = NormalizePath(trimmedPath);
        
        return normalizedPath.Substring(normalizedPath.LastIndexOf(DirectorySeparatorChar) + 1);
    }
}
