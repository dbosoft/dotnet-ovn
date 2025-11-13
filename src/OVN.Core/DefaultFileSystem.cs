using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;

namespace Dbosoft.OVN;

[PublicAPI]
public class DefaultFileSystem : IFileSystem
{
    private readonly OSPlatform _platform;

    public DefaultFileSystem(OSPlatform platform)
    {
        _platform = platform;
    }

    [ExcludeFromCodeCoverage]
    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    [ExcludeFromCodeCoverage]
    public bool FileExists(OvsFile ovsFile)
    {
        var fullPath = ResolveOvsFilePath(ovsFile, false);
        return FileExists(fullPath);
    }

    public string ResolveOvsFilePath(OvsFile file, bool platformNeutral = true)
    {
        var basePath = "";
        var pathRoot = file.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        basePath = ResolveBasePath(pathRoot.Length == 0
            ? file.Path
            : pathRoot[0]);

        if (pathRoot.Length > 1)
            for (var i = 1; i < pathRoot.Length; i++)
                basePath = $"{basePath}/{pathRoot[i]}";

        var fileName = file.IsExe && _platform == OSPlatform.Windows
            ? $"{file.Name}.exe"
            : file.Name;

        var resolvedPath = $"{basePath}/{fileName}";

        return platformNeutral
            ? resolvedPath
            : ConvertPathToPlatform(resolvedPath);
    }

    [ExcludeFromCodeCoverage]
    public void EnsurePathForFileExists(string path)
    {
        var platformPath = ConvertPathToPlatform(path);
        platformPath = Path.GetDirectoryName(platformPath);

        if (platformPath == null)
            return;

        var directoryInfo = new DirectoryInfo(platformPath);
        if (!directoryInfo.Exists)
            directoryInfo.Create();
    }
    
    public void EnsurePathForFileExists(OvsFile file)
    {
        var path = ResolveOvsFilePath(file);
        EnsurePathForFileExists(path);
    }
    
    public string ReadFileAsString(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(stream);
        return sr.ReadToEnd();
    }

    private string ConvertPathToPlatform(string inputPath)
    {
        var platformSeparator = _platform != OSPlatform.Windows
            ? '/'
            : '\\';

        if (platformSeparator != '/')
            inputPath = inputPath.Replace('/', platformSeparator);

        return inputPath;
    }

    private string ResolveBasePath(string pathRoot)
    {
        return pathRoot.StartsWith("usr")
            ? $"{GetProgramRootPath()}{pathRoot}"
            : $"{GetDataRootPath()}{pathRoot}";
    }

    protected virtual string GetDataRootPath() =>
        _platform == OSPlatform.Windows
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "openvswitch").Replace(@"\", "/") + "/"
            : "/";

    protected virtual string GetProgramRootPath() =>
        _platform == OSPlatform.Windows ? "C:/openvswitch/" : "/";

    public async Task<string> ReadFileAsync(string path)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(stream, Encoding.UTF8);
        return await sr.ReadToEndAsync();
    }

    public async Task<string> ReadFileAsync(OvsFile file)
    {
        var path = ResolveOvsFilePath(file);
        return await ReadFileAsync(path);
    }

    public async Task WriteFileAsync(string path, string content)
    {
        await using var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        await using var sw = new StreamWriter(stream, Encoding.UTF8);
        await sw.WriteAsync(content);
    }

    public Task WriteFileAsync(OvsFile file, string content)
    {
        var path = ResolveOvsFilePath(file);
        return WriteFileAsync(path, content);
    }
}
