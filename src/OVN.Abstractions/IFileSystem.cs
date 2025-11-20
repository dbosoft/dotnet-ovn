namespace Dbosoft.OVN;

/// <summary>
/// File system abstraction
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Returns true if the file exists. File path is expected in native os format.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>File exists</returns>
    bool FileExists(string filePath);

    /// <summary>
    /// Returns true if the file exists.
    /// </summary>
    /// <param name="ovsFile">file </param>
    /// <returns>File exists</returns>
    bool FileExists(OvsFile ovsFile);
    
    /// <summary>
    /// Resolves the path of file. Platform neutral format will be in unix slash style.
    /// </summary>
    /// <param name="file">file</param>
    /// <param name="platformNeutral">path will be in unix slash style.</param>
    /// <returns>the full path to file</returns>
    string ResolveOvsFilePath(OvsFile file, bool platformNeutral = true);

    /// <summary>
    /// Ensures that the path (parent directories) exists for the path.
    /// Will only work for file paths and not for directories.
    /// </summary>
    /// <param name="path">
    /// The path of the file.
    /// </param>
    /// <param name="adminOnly">
    /// Indicates that the access to the directory should be restricted to administrators.
    /// </param>
    void EnsurePathForFileExists(string path, bool adminOnly = false);

    /// <summary>
    /// Ensures that the path (parent directories) exists for the path.
    /// Will only work for file paths and not for directories.
    /// </summary>
    /// <param name="file">
    /// The OVS file
    /// </param>
    /// <param name="adminOnly">
    /// Indicates that the access to the directory should be restricted to administrators.
    /// </param>
    void EnsurePathForFileExists(OvsFile file, bool adminOnly = false);
    
    Task<string> ReadFileAsync(string path, CancellationToken cancellationToken = default);

    Task<string> ReadFileAsync(OvsFile file, CancellationToken cancellationToken = default);

    Task WriteFileAsync(string path, string content, CancellationToken cancellationToken = default);

    Task WriteFileAsync(OvsFile file, string content, CancellationToken cancellationToken =default);
}
