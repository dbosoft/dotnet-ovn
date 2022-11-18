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
    /// <param name="path">path of file</param>
    void EnsurePathForFileExists(string path);
    
    /// <summary>
    /// Ensures that the path (parent directories) exists for the path.
    /// Will only work for file paths and not for directories.
    /// </summary>
    /// <param name="file">OVS file</param>
    void EnsurePathForFileExists(OvsFile file);

    /// <summary>
    /// read the content of file as string
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    string ReadFileAsString(string path);
}