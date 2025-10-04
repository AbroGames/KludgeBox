// ReSharper disable UnusedMember.Global

namespace KludgeBox.VFS.Base;

public class FsDirectory : FsEntry
{
    public FsEntry[] Entries => FileSystem.GetEntries(Path);
    public FsFile[] Files => FileSystem.GetFiles(Path);
    public FsDirectory[] Directories => FileSystem.GetDirectories(Path);
    public bool IsRoot => FileSystem.Root.Equals(this);
    public bool IsEmpty => FileSystem.IsEmpty(Path);
    public override bool Exists => FileSystem.IsDirectory(Path);

    public FsDirectory(FileSystem fileSystem, string path) : base(fileSystem, path)
    {
        Path = path.NormalizePath().EnsureEndingDirectorySeparator();
    }

    public static FsDirectory FromEntry(FsEntry entry) => entry.FileSystem.GetDirectory(entry.Path);
    public FsDirectory CreateDirectory(string name, bool recursive = true)
    {
        FileSystem.DoReadOnlyCheck();
        return FileSystem.CreateDirectory(PathHelper.Combine(Path, name));
    }

    public FsFile CreateFile(string name)
    {
        FileSystem.DoReadOnlyCheck();
        return FileSystem.CreateFile(PathHelper.Combine(Path, name));
    }

    public FsFile CreateFile(string name, byte[] data)
    {
        FileSystem.DoReadOnlyCheck();
        var file = FileSystem.CreateFile(PathHelper.Combine(Path, name));
        if(data != null)
            file.WriteAllBytes(data);

        return file;
    }

    public FsFile CreateFile(string name, string content)
    {
        FileSystem.DoReadOnlyCheck();
        var file = FileSystem.CreateFile(PathHelper.Combine(Path, name));
        if(!String.IsNullOrEmpty(content))
            file.WriteAllText(content);

        return file;
    }

    public void Delete(bool recursive = false)
    {
        FileSystem.DoReadOnlyCheck();
        FileSystem.DeleteDirectory(Path, recursive);
    }
    public bool HasEntry(string name) => FileSystem.Exists(PathHelper.Combine(Path, name));
    public bool HasFile(string name) => FileSystem.IsFile(PathHelper.Combine(Path, name));
    public bool HasDirectory(string name) => FileSystem.IsDirectory(PathHelper.Combine(Path, name));

    public FsDirectory[] GetDirectories() => FileSystem.GetDirectories(Path);
    public FsFile[] GetFiles() => FileSystem.GetFiles(Path);

    public FsFile[] FlattenFiles()
    {
        var result = new List<FsFile>();
        result.AddRange(GetFiles());
        foreach (var dir in GetDirectories())
        {
            result.AddRange(dir.FlattenFiles());
        }
        
        return result.ToArray();
    }

    public FsEntry[] GetEntries() => FileSystem.GetEntries(Path);

    public FsFile GetFile(string name, bool softChecks = false) => FileSystem.GetFile(PathHelper.Combine(Path, name), softChecks);
    public FsDirectory GetDirectory(string name) => FileSystem.GetDirectory(PathHelper.Combine(Path, name));
    
    public FsEntry GetEntry(string name) => FileSystem.GetEntry(PathHelper.Combine(Path, name));

    /// <summary>
    /// Reads the contents of the specified file as UTF-8 encoded text.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public string ReadAllText(string path) => GetFile(path).ReadAllText();

    /// <summary>
    /// Reads all bytes from the specified file.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public byte[] ReadAllBytes(string path) => GetFile(path).ReadAllBytes();
    
    public Stream OpenRead(string path) => GetFile(path).OpenRead();

    /// <summary>
    /// Returns writeable stream for the file, if possible.
    /// </summary>
    /// <param name="path">Filename or local path to file</param>
    /// <returns></returns>
    public Stream OpenWrite(string path)
    {
        return GetFile(path, true).OpenWrite();
    }

    /// <summary>
    /// Copies directory to specified path. Throws exception, path already exists.
    /// </summary>
    /// <param name="destinationPath"></param>
    public void CopyTo(string destinationPath)
    {
        FileSystem.DoReadOnlyCheck();
        FileSystem.CopyDirectory(Path, destinationPath);
    }

    /// <summary>
    /// Copy contents of directory into other directory.
    /// </summary>
    /// <param name="other"></param>
    public void CopyContentsTo(FsDirectory other)
    {
        FileSystem.TransCopyDir(this, other);
    }
}
