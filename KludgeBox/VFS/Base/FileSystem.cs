using System.Text;

namespace KludgeBox.VFS.Base;

/// <summary>
/// Abstract base class for all VFS implementations.
/// </summary>
public abstract class FileSystem
{
    /// <summary>
    /// Gets the root directory of the file system.
    /// </summary>
    public virtual FsDirectory Root => new FsDirectory(this, "/");

    /// <summary>
    /// Gets a value indicating whether the file system is read-only.
    /// </summary>
    public virtual bool IsReadOnly { get; } = true;

    public virtual string Name { get; protected init; } = "FileSystem";

    
    #region Abstract Methods
    /// <summary>
    /// Recursively creates a directory at the specified path.
    /// </summary>
    /// <param name="path">The path to the directory to create.</param>
    /// <returns>The created Directory object.</returns>
    public abstract FsDirectory CreateDirectory(string path);
    
    /// <summary>
    /// Deletes a directory at the specified path.
    /// </summary>
    /// <param name="path">The path to the directory to delete.</param>
    /// <param name="recursive">True to delete all child directories and files.</param>
    public abstract void DeleteDirectory(string path, bool recursive = false);
    
    /// <summary>
    /// Creates a file at the specified path.
    /// </summary>
    /// <param name="path">The path to the file to create.</param>
    /// <returns>The created File object.</returns>
    public abstract FsFile CreateFile(string path);

    /// <summary>
    /// Deletes a file at the specified path.
    /// </summary>
    /// <param name="path">The path to the file to delete.</param>
    public abstract void DeleteFile(string path);
    
    /// <summary>
    /// Opens a stream for reading from a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>A stream for reading the file contents.</returns>
    public abstract Stream OpenRead(string path);

    /// <summary>
    /// Opens a stream for writing to a file. Creates file if doesn't exist.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>A stream for writing to the file.</returns>
    public abstract Stream OpenWrite(string path);
    
    /// <summary>
    /// Gets an array of File objects for the specified directory path.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <returns>An array of File objects.</returns>
    public abstract FsFile[] GetFiles(string path);

    /// <summary>
    /// Gets an array of Directory objects for the specified directory path.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <returns>An array of Directory objects.</returns>
    public abstract FsDirectory[] GetDirectories(string path);
    public abstract bool IsDirectory(string path);
    public abstract bool IsFile(string path);
    #endregion
    
    #region Virtual Methods
    /// <summary>
    /// Moves a file from one location to another.
    /// </summary>
    /// <param name="sourcePath">The exact source file path</param>
    /// <param name="destinationPath">The exact destination file path</param>
    /// <param name="overwrite">Indicates whether to overwrite an existing file if needed</param>
    /// <remarks>It is recommended to consider optimizing the implementation for performance.</remarks>
    public virtual void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        // Yes, this is slow AF.
        CopyFile(sourcePath, destinationPath, overwrite);
        DeleteFile(sourcePath);
    }


    public virtual bool IsEmpty(string path)
    {
        DoDirectoryCheck(path);
        return GetEntries(path).Length == 0;
    }

    /// <summary>
    /// Copies a file from one location to another.
    /// </summary>
    /// <param name="sourcePath">The exact source file path</param>
    /// <param name="destinationPath">The exact destination file path</param>
    /// <param name="overwrite">Indicates whether to overwrite an existing file if needed</param>
    public virtual void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        DoFileCheck(sourcePath); // check if source exists
        DoFileCheck(destinationPath, true); // check if destination file exists or can be created
        if (Exists(destinationPath) && !overwrite) // overwrite check
            throw new IOException($"{destinationPath} already exists");

        using var reader = OpenRead(sourcePath);
        using var writer = OpenWrite(destinationPath);

        reader.CopyTo(writer);
        writer.Flush();
    }
    
    /// <summary>
    /// Moves a directory from one location to another.
    /// </summary>
    /// <param name="sourcePath">The exact source directory path</param>
    /// <param name="destinationPath">The exact destination directory path</param>
    /// <remarks>It is recommended to consider optimizing the implementation for performance.</remarks>
    public virtual void MoveDirectory(string sourcePath, string destinationPath)
    {
        // If you thought the MoveFile() was slow, then how about this:
        CopyDirectory(sourcePath, destinationPath);
        DeleteDirectory(sourcePath);
    }
    
    /// <summary>
    /// Copies a directory from one location to another.
    /// </summary>
    /// <param name="sourcePath">The exact source directory path</param>
    /// <param name="destinationPath">The exact destination directory path</param>
    public virtual void CopyDirectory(string sourcePath, string destinationPath)
    {
        DoDirectoryCheck(sourcePath);
        if (Exists(destinationPath))
            throw new IOException($"{destinationPath} already exists");

        var entries = GetEntries(sourcePath);
        CreateDirectory(destinationPath);
        foreach (var entry in entries)
        {
            var entryName = entry.Name;
            var entryDestinationPath = Path.Combine(destinationPath, entryName);
            if(entry is FsFile)
                CopyFile(entry.Path, entryDestinationPath);
            
            if(entry is FsDirectory)
                CopyDirectory(entry.Path, entryDestinationPath);
        }
    }

    /// <summary>
    /// Gets an entry (either a file or directory) at the specified path.
    /// </summary>
    /// <param name="path">The path to the entry</param>
    /// <returns>The entry object if found</returns>
    public virtual FsEntry GetEntry(string path)
    {
        var entries = GetEntries(PathHelper.GetParent(path));
        var entry = entries.FirstOrDefault(e => e.Path == path);
        if (entry is null)
            throw new FileNotFoundException(path);

        return entry;
    }
    
    /// <summary>
    /// Gets the closest existing parent directory path for a given path.
    /// </summary>
    /// <param name="path">The path to find the closest parent directory for</param>
    /// <returns>The closest parent directory path</returns>
    public virtual string GetClosestParent(string path)
    {
        if (IsDirectory(path)) 
            return path;
        
        return GetClosestParent(PathHelper.GetParent(path));
    }

    /// <summary>
    /// Gets filtered entries (directories and files) in the specified path.
    /// </summary>
    /// <param name="path">The path to get filtered entries from</param>
    /// <returns>A tuple containing arrays of directories and files</returns>
    public virtual (FsDirectory[] directories, FsFile[] files) GetFilteredEntries(string path)
    {
        var directories = new List<FsDirectory>();
        var files = new List<FsFile>();

        foreach (var entry in GetEntries(path))
        {
            if(entry.IsDirectory)
                directories.Add(new FsDirectory(this, entry.Path));
            else
                files.Add(new FsFile(this, entry.Path));
        }
        
        return (directories.ToArray(), files.ToArray());
    }
    
    /// <summary>
    /// Gets a Directory object for the specified path.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <returns>The Directory object if it exists, otherwise null.</returns>
    public virtual FsDirectory GetDirectory(string path)
    {
        DoDirectoryCheck(path);
        return new FsDirectory(this, path);
    }

    /// <summary>
    /// Gets a File object for the specified path.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="softChecks">Will return file instance even if it does not exist.</param>
    /// <returns>The File object if it exists, otherwise null.</returns>
    public virtual FsFile GetFile(string path, bool softChecks = false)
    {
        DoFileCheck(path, softChecks);
        
        return new FsFile(this, path);
    }

    /// <summary>
    /// Checks if a file or directory exists at the specified path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the file or directory exists, otherwise false.</returns>
    public virtual bool Exists(string path)
    {
        return IsDirectory(path) || IsFile(path);
    }
    
    /// <summary>
    /// Throws an exception if the file system is read-only.
    /// </summary>
    /// <exception cref="NotSupportedException"></exception>
    public void DoReadOnlyCheck()
    {
        if(IsReadOnly)
            throw new NotSupportedException("This file system is read-only.");
    }
    
    /// <summary>
    /// Returns the parent directory of the specified path.
    /// </summary>
    /// <param name="path">The path for which to find the parent directory.</param>
    /// <returns>The parent Directory object.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the specified path does not exist.</exception>
    /// <remarks>For the root directory, it returns itself.</remarks>
    public virtual FsDirectory GetParent(string path)
    {
        if (!Exists(path))
            throw new FileNotFoundException($"File or directory not found:\nVFS: {path}");
        
        return GetDirectory(PathHelper.GetParent(path));
    }

    /// <summary>
    /// Gets an array of FsEntry objects for the specified path. Actual type of every type must be an file or directory.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <returns>An array of FsEntry objects.</returns>
    /// <remarks>Default implementation seems to be optimal</remarks>
    public virtual FsEntry[] GetEntries(string path)
    {
        List<FsEntry> entries = new List<FsEntry>();
        entries.AddRange(GetDirectories(path));
        entries.AddRange(GetFiles(path));

        return entries.ToArray();
    }

    /// <summary>
    /// Performs a file check for the specified path.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="soft">If set to true, performs a soft check that will not throw exceptions if the file can be created (parent directory exists and the path is not a directory).</param>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown if the parent directory does not exist (in soft check).</exception>
    /// <exception cref="IOException">Thrown if the given path points to a directory.</exception>
    public virtual void DoFileCheck(string path, bool soft = false)
    {
        if (!soft && !IsFile(path))
            throw new FileNotFoundException(path);

        var parent = PathHelper.GetParent(path);
        if (!IsDirectory(parent))
            throw new DirectoryNotFoundException(parent);
        
        if (soft && IsDirectory(path))
            throw new IOException($"{path} is directory");
    }

    /// <summary>
    /// Performs a directory check for the specified path.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <param name="soft">If set to true, performs a soft check that will not throw exceptions if the directory can be created (parent directory exists and the path is not a file).</param>
    /// <exception cref="DirectoryNotFoundException">Thrown if the directory does not exist, or its parent directory does not exist in the soft check.</exception>
    /// <exception cref="IOException">Thrown if the given path points to a file.</exception>
    public virtual void DoDirectoryCheck(string path, bool soft = false)
    {
        if (!soft && !IsDirectory(path))
            throw new DirectoryNotFoundException(path);

        var parent = PathHelper.GetParent(path);
        if (!IsDirectory(parent))
            throw new DirectoryNotFoundException(parent);
        
        if (soft && IsFile(path))
            throw new IOException($"{path} is file");
    }
    
    /// <summary>
    /// Reads all bytes from a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>An array of bytes representing the file content.</returns>
    public virtual byte[] ReadAllBytes(string path)
    {
        DoFileCheck(path);
        using var stream = OpenRead(path);
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
    
    /// <summary>
    /// Reads all text from a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>The text content of the file.</returns>
    public virtual string ReadAllText(string path)
    {
        byte[] data = ReadAllBytes(path);
        return Encoding.UTF8.GetString(data);
    }
    
    /// <summary>
    /// Writes all bytes to a file, overwriting if it exists.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="bytes">The bytes to write.</param>
    public virtual void WriteAllBytes(string path, byte[] bytes)
    {
        if (IsDirectory(path))
            throw new InvalidOperationException($"{path} is a directory");
        
        if(!IsFile(path))
            CreateFile(path);

        using (var stream = OpenWrite(path))
        {
            stream.Write(bytes);
            stream.Flush();
        }
    }

    
    /// <summary>
    /// Writes all text to a file, overwriting if it exists.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="text">The text to write.</param>
    public virtual void WriteAllText(string path, string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);
        WriteAllBytes(path, data);
    }


    /// <summary>
    /// Appends text to a file, creating the file if it does not exist.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="text">The text to append.</param>
    /// <exception cref="FileNotFoundException"></exception>
    public virtual void AppendText(string path, string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        AppendBytes(path, bytes);
    }

    /// <summary>
    /// Appends array of bytes to the end of file. Creates file if it does not exist.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="bytes"></param>
    /// <remarks>Very heavy default implementation. Consider overriding it.</remarks>
    public virtual void AppendBytes(string path, byte[] bytes)
    {
        if (IsDirectory(path))
            throw new InvalidOperationException($"{path} is a directory");

        if (!IsFile(path))
            CreateFile(path);
        
        using Stream readStream = OpenRead(path);
        using Stream writeStream = OpenWrite(path);
        readStream.CopyTo(writeStream);
        writeStream.Write(bytes);
        writeStream.Flush();
        readStream.Close();
        writeStream.Close();
    }
    #endregion

    public override string ToString()
    {
        return $"{Name}";
    }
    
    public static void TransCopyDir(FsDirectory from, FsDirectory to)
    {
        to.FileSystem.DoReadOnlyCheck();
        
        if (!from.Exists)
            throw new DirectoryNotFoundException(from.Path);

        if (from.Equals(to))
            return;

        if (!to.Exists)
            to.FileSystem.CreateDirectory(to.Path);
        
        foreach (var fsDirectory in from.GetDirectories())
        {
            TransCopyDir(fsDirectory, to.CreateDirectory(fsDirectory.Name));
        }

        foreach (var fromFile in from.GetFiles())
        {
            var toFile = to.CreateFile(fromFile.Name);
            using var fromStream = fromFile.OpenRead();
            using var toStream = toFile.OpenWrite();

            fromStream.CopyTo(toStream);
            toStream.Flush();
        }
    }
}
