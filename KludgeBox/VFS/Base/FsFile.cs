namespace KludgeBox.VFS.Base;

public class FsFile : FsEntry
{
    public string NameWithoutExtension => PathHelper.HasExtension(Name) ? Name.Substring(0, Name.LastIndexOf('.')) : Name;
    public string Extension => PathHelper.HasExtension(Name) ? Name.Substring(Name.LastIndexOf('.') + 1) : string.Empty;

    public FsFile(FileSystem filesystem, string name) : base(filesystem, name)
    {
        
    }

    public static FsFile FromEntry(FsEntry entry) => entry.FileSystem.GetFile(entry.Path);
    public byte[] ReadAllBytes() => FileSystem.ReadAllBytes(Path);
    public string ReadAllText() => FileSystem.ReadAllText(Path);
    public override bool Exists => FileSystem.IsFile(Path);
    
    public void WriteAllBytes(byte[] data)
    { 
        FileSystem.DoReadOnlyCheck();
        FileSystem.WriteAllBytes(Path, data);
    }

    public void AppendText(string text)
    {
        FileSystem.DoReadOnlyCheck();
        FileSystem.AppendText(Path, text);
    }

    public void WriteAllText(string text)
    {
        FileSystem.DoReadOnlyCheck();
        FileSystem.WriteAllText(Path, text);
    }

    public void CopyTo(string destinationPath)
    {
        FileSystem.DoReadOnlyCheck();
        FileSystem.CopyFile(Path, destinationPath);
    }

    public void MoveTo(string destinationPath, bool overwrite = false)
    {
        FileSystem.DoReadOnlyCheck();
        FileSystem.MoveFile(Path, destinationPath, overwrite);
    }

    public void Delete()
    {
        FileSystem.DoReadOnlyCheck();
        FileSystem.DeleteFile(Path);
    }

    public void CopyTo(string destinationPath, bool overwrite = false)
    {
        FileSystem.DoReadOnlyCheck();
        FileSystem.CopyFile(Path, destinationPath, overwrite);
    }

    public void CopyTo(FsDirectory fsDirectory, string name = null, bool overwrite = false)
    {
        fsDirectory.FileSystem.DoReadOnlyCheck();
        if (fsDirectory.HasFile(name ?? Name))
        {
            if (overwrite)
            {
                WriteTo(fsDirectory.GetFile(name ?? Name));
            }
        }

        var file = fsDirectory.CreateFile(name ?? Name);
        WriteTo(file);
    }

    public void WriteTo(FsFile file)
    {
        using (var writeStream = file.OpenWrite())
        {
            using (var readStream = OpenRead())
            {
                readStream.CopyTo(writeStream);
                writeStream.Flush();
            }
        }
    }
    
    public Stream OpenRead() => FileSystem.OpenRead(Path);

    public Stream OpenWrite()
    {
        FileSystem.DoReadOnlyCheck();
        return FileSystem.OpenWrite(Path);
    }
}
