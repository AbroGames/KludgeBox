using KludgeBox.VFS.Base;

namespace KludgeBox.VFS.FileSystems;

/// <summary>
/// Provides access to the part of another FileSystem.
/// </summary>
public class ProxyFileSystem : FileSystem, IProxyFileSystem
{
    public FileSystem TargetFileSystem { get; }

    public override FsDirectory Root => new FsDirectory(this,"/");
    public override bool IsReadOnly => TargetFileSystem.IsReadOnly;
    private static int _nextId = 0;

    public string RealPath => _root;
    
    private string _root;

    public ProxyFileSystem(FileSystem targetFileSystem, string rootPath)
    {
        TargetFileSystem = targetFileSystem;
        _root = rootPath;
        Name = $"ProxyFS #{_nextId++} (for {targetFileSystem.Name})";
    }

    public ProxyFileSystem(FsDirectory destination) : this(destination.FileSystem, destination.Path)
    {
        
    }


    public override byte[] ReadAllBytes(string path) => 
        TargetFileSystem.ReadAllBytes(PathHelper.Combine(_root, path));

    public override string ReadAllText(string path) => 
        TargetFileSystem.ReadAllText(PathHelper.Combine(_root, path));

    public override void WriteAllBytes(string path, byte[] bytes) => 
        TargetFileSystem.WriteAllBytes(PathHelper.Combine(_root, path), bytes);

    public override void WriteAllText(string path, string text) => 
        TargetFileSystem.WriteAllText(PathHelper.Combine(_root, path), text);

    public override void AppendText(string path, string text) => 
        TargetFileSystem.AppendText(PathHelper.Combine(_root, path), text);

    public override FsDirectory CreateDirectory(string path)
    {
        TargetFileSystem.CreateDirectory(PathHelper.Combine(_root, path));
        return new FsDirectory(this, path);
    }

    public override void DeleteDirectory(string path, bool recursive = false) => 
        TargetFileSystem.DeleteDirectory(PathHelper.Combine(_root, path), recursive);

    public override FsFile CreateFile(string path)
    {
        TargetFileSystem.CreateFile(PathHelper.Combine(_root, path));
        return new FsFile(this, path);
    }
    
    public override void DeleteFile(string path) => 
        TargetFileSystem.DeleteFile(PathHelper.Combine(_root, path));

    public override Stream OpenRead(string path) =>
        TargetFileSystem.OpenRead(PathHelper.Combine(_root, path));

    public override Stream OpenWrite(string path) =>
        TargetFileSystem.OpenWrite(PathHelper.Combine(_root, path));

    public override FsDirectory GetDirectory(string path)
    {
        TargetFileSystem.GetDirectory(PathHelper.Combine(_root, path));
        return new FsDirectory(this, path);
    }

    public override FsFile GetFile(string path, bool softChecks = false)
    {
        TargetFileSystem.GetFile(PathHelper.Combine(_root, path), softChecks);
        return new FsFile(this, path);
    }

    public override bool Exists(string path) =>
        TargetFileSystem.Exists(PathHelper.Combine(_root, path));

    public override FsEntry[] GetEntries(string path)
    {
        var targetEntries = TargetFileSystem.GetEntries(PathHelper.Combine(_root, path));
        var entries = new List<FsEntry>();
        foreach (var targetEntry in targetEntries)
        {
            entries.Add(new FsEntry(this, PathHelper.Combine(path, targetEntry.Name)));
        }
        return entries.ToArray();
    }

    public override FsFile[] GetFiles(string path)
    {
        var targetFiles = TargetFileSystem.GetFiles(PathHelper.Combine(_root, path));
        var files = new List<FsFile>();
        foreach (var targetFile in targetFiles)
        {
            files.Add(new FsFile(this, PathHelper.Combine(path, targetFile.Name)));
        }
        return files.ToArray();
    }

    public override FsDirectory[] GetDirectories(string path)
    {
        var targetDirectories = TargetFileSystem.GetDirectories(PathHelper.Combine(_root, path));
        var directories = new List<FsDirectory>();
        foreach (var targetDirectory in targetDirectories)
        {
            directories.Add(new FsDirectory(this, PathHelper.Combine(path, targetDirectory.Name).EnsureEndingDirectorySeparator()));
        }
        return directories.ToArray();
    }

    public override void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        TargetFileSystem.MoveFile(
            PathHelper.Combine(_root, sourcePath), 
            PathHelper.Combine(_root, destinationPath), 
            overwrite);
    }

    public override void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        TargetFileSystem.CopyFile(
            PathHelper.Combine(_root, sourcePath), 
            PathHelper.Combine(_root, destinationPath), 
            overwrite);
    }

    public override void MoveDirectory(string sourcePath, string destinationPath)
    {
        TargetFileSystem.MoveDirectory(
            PathHelper.Combine(_root, sourcePath), 
            PathHelper.Combine(_root, destinationPath));
    }

    public override void CopyDirectory(string sourcePath, string destinationPath)
    {
        TargetFileSystem.CopyDirectory(
            PathHelper.Combine(_root, sourcePath), 
            PathHelper.Combine(_root, destinationPath));
    }

    public override bool IsDirectory(string path)
    {
        return TargetFileSystem.IsDirectory(PathHelper.Combine(_root, path));
    }

    public override bool IsFile(string path)
    {
        return TargetFileSystem.IsFile(PathHelper.Combine(_root, path));
    }

    public override FsEntry GetEntry(string path)
    {
        TargetFileSystem.GetEntry(PathHelper.Combine(_root, path));
        return new FsEntry(this, path);
    }
}
