using KludgeBox.VFS.Base;

namespace KludgeBox.VFS.FileSystems;

/// <summary>
/// Provides access to the local file system.
/// </summary>
public class PhysicalFileSystem : FileSystem, IProxyFileSystem
{
    private string _root;
    public override bool IsReadOnly => false;
    public string RealPath => _root;
    private static int _nextId = 0;

    public PhysicalFileSystem(string root)
    {
        _root = PathHelper.NormalizePath(root);
        Name = $"PhysFS #{_nextId++} @ {Path.GetRelativePath(Directory.GetCurrentDirectory(), _root)}";
    }
    public override byte[] ReadAllBytes(string path) => 
        File.ReadAllBytes(this.GetRealPath(path));

    public override string ReadAllText(string path) => 
        File.ReadAllText(this.GetRealPath(path));

    public override void WriteAllBytes(string path, byte[] bytes) => 
        File.WriteAllBytes(this.GetRealPath(path), bytes);

    public override void WriteAllText(string path, string text) =>
        File.WriteAllText(this.GetRealPath(path), text);
    

    public override void AppendText(string path, string text) => 
        File.AppendAllText(this.GetRealPath(path), text);

    public override FsDirectory CreateDirectory(string path)
    {
        var dirInfo = Directory.CreateDirectory(this.GetRealPath(path));
        return GetDirectory(path);
    }

    public override void DeleteDirectory(string path, bool recursive = false)
    {
        if (path.IsRoot())
            throw new IOException($"Attempt to delete the root of {Name}");
        
        DoDirectoryCheck(path);
        InternalDeleteDirectory(new DirectoryInfo(this.GetRealPath(path)), recursive);
    }

    private static void InternalDeleteDirectory(DirectoryInfo baseDir, bool recursive = false)
    {
        if (!baseDir.Exists)
            throw new DirectoryNotFoundException(baseDir.FullName);
        
        if (recursive)
            foreach (var dir in baseDir.EnumerateDirectories())
                InternalDeleteDirectory(dir, true);
        
        Directory.Delete(baseDir.FullName, recursive);
    }
    
    public override FsFile CreateFile(string path)
    {
        var trimmedPath = path.TrimEndingDirectorySeparator();
        File.Create(this.GetRealPath(trimmedPath)).Dispose();
        return GetFile(trimmedPath);
    }

    public override void DeleteFile(string path) =>
        File.Delete(this.GetRealPath(path));

    public override Stream OpenRead(string path)
    {
        return File.OpenRead(this.GetRealPath(path));
    }

    public override Stream OpenWrite(string path)
    {
        return File.OpenWrite(this.GetRealPath(path));
    }

    public override FsDirectory GetDirectory(string path)
    {
        DoDirectoryCheck(path);
        
        return new FsDirectory(this, path);
    }

    public override FsFile GetFile(string path, bool softChecks = false)
    {
        DoFileCheck(path, softChecks);
        
        return new FsFile(this, path);
    }
    
    public override FsEntry GetEntry(string path)
    {
        if(!Exists(path))
            throw new FileNotFoundException(path);

        return new FsEntry(this, path);
    }

    public override bool Exists(string path)
    {
        var fileExists = File.Exists(this.GetRealPath(path));
        var directoryExists = Directory.Exists(this.GetRealPath(path));
        
        return fileExists || directoryExists;
    }

    public override bool IsDirectory(string path) => Directory.Exists(this.GetRealPath(path));
    public override bool IsFile(string path) => File.Exists(this.GetRealPath(path));

    public override FsEntry[] GetEntries(string path)
    {
        DoDirectoryCheck(path);
        
        var entries = new List<FsEntry>();
        entries.AddRange(GetDirectories(path));
        entries.AddRange(GetFiles(path));
        
        return entries.ToArray();
    }

    public override FsFile[] GetFiles(string path)
    {
        DoDirectoryCheck(path);
        var files = new List<FsFile>();
        foreach (var entry in Directory.GetFiles(this.GetRealPath(path)))
        {
            files.Add(new FsFile(this, PathHelper.Combine(path, PathHelper.GetName(entry))));
        }
        
        return files.ToArray();
    }

    public override FsDirectory[] GetDirectories(string path)
    {
        DoDirectoryCheck(path);
        
        var directories = new List<FsDirectory>();
        foreach (var entry in Directory.GetDirectories(this.GetRealPath(path)))
        {
            var dir = PathHelper.Combine(path, entry.GetName()).EnsureEndingDirectorySeparator();
            directories.Add(new FsDirectory(this, dir));
        }
        
        return directories.ToArray();
    }

    public override void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        File.Move(
            this.GetRealPath(sourcePath), 
            this.GetRealPath(destinationPath),
            overwrite);
    }

    public override void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        File.Copy(
            this.GetRealPath(sourcePath), 
            this.GetRealPath(destinationPath),
            overwrite);
    }

    public override void MoveDirectory(string sourcePath, string destinationPath)
    {
        if (sourcePath.IsRoot())
            throw new IOException($"Attempt to move the root of {Name}");
        
        Directory.Move(
            this.GetRealPath(sourcePath), 
            this.GetRealPath(destinationPath));
    }
    
    
    public override void CopyDirectory(string sourcePath, string destinationPath)
    {
        if(IsDirectory(destinationPath))
            throw new IOException($"{destinationPath} already exists.");
        
        var files = GetFiles(sourcePath);
        var dirs = GetDirectories(sourcePath);
        
        var directory = CreateDirectory(destinationPath);
        foreach (var file in files)
            file.CopyTo(PathHelper.Combine(destinationPath, file.Name));

        foreach (var dir in dirs)
            directory.CopyTo(PathHelper.Combine(destinationPath, dir.Name));
    }
}
