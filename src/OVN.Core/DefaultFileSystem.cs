using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
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
    public void EnsurePathForFileExists(string path, bool adminOnly = false)
    {
        var platformPath = ConvertPathToPlatform(path);
        platformPath = Path.GetDirectoryName(platformPath);

        if (platformPath is null)
            throw new ArgumentException("The path does not contain a valid directory.", nameof(path));

        var directoryInfo = new DirectoryInfo(platformPath);
        directoryInfo.Create();

        if (!adminOnly)
            return;
        
        SetAdminOnlyPermissions(directoryInfo);
    }

    public void EnsurePathForFileExists(OvsFile file, bool adminOnly = false)
    {
        var path = ResolveOvsFilePath(file);
        EnsurePathForFileExists(path);
    }

    public void DeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            // Ignore the exception when the path does not exist
        }
    }

    public void DeleteFile(OvsFile file)
    {
        var path = ResolveOvsFilePath(file);
        DeleteFile(path);
    }

    public async Task<string> ReadFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(stream, Encoding.UTF8);
        return await sr.ReadToEndAsync(cancellationToken);
    }

    public async Task<string> ReadFileAsync(
        OvsFile file,
        CancellationToken cancellationToken = default)
    {
        var path = ResolveOvsFilePath(file);
        return await ReadFileAsync(path, cancellationToken);
    }

    public async Task WriteFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        await using var sw = new StreamWriter(stream, Encoding.UTF8);
        await sw.WriteAsync(new StringBuilder(content), cancellationToken);
    }

    public Task WriteFileAsync(
        OvsFile file,
        string content,
        CancellationToken cancellationToken = default)
    {
        var path = ResolveOvsFilePath(file);
        return WriteFileAsync(path, content, cancellationToken);
    }

    protected virtual string GetDataRootPath() =>
        _platform == OSPlatform.Windows
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "openvswitch").Replace(@"\", "/") + "/"
            : "/";

    protected virtual string GetProgramRootPath() =>
        _platform == OSPlatform.Windows ? "C:/openvswitch/" : "/";

    protected virtual void SetAdminOnlyPermissions(
        DirectoryInfo directoryInfo)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                directoryInfo.FullName,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            return;
        }

        var directorySecurity = new DirectorySecurity();
        IdentityReference adminId = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        var adminAccess = new FileSystemAccessRule(
            adminId,
            FileSystemRights.FullControl,
            InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
            PropagationFlags.None,
            AccessControlType.Allow);

        IdentityReference systemId = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
        var systemAccess = new FileSystemAccessRule(
            systemId,
            FileSystemRights.FullControl,
            InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
            PropagationFlags.None,
            AccessControlType.Allow);

        directorySecurity.AddAccessRule(adminAccess);
        directorySecurity.AddAccessRule(systemAccess);
        // Set the owner and the group to admins
        directorySecurity.SetAccessRuleProtection(true, true);
        
        directoryInfo.SetAccessControl(directorySecurity);
    }

    private DirectoryInfo CreateDirectoryInfo(string path)
    {
        var platformPath = ConvertPathToPlatform(path);
        platformPath = Path.GetDirectoryName(platformPath);

        return platformPath is not null
            ? new DirectoryInfo(platformPath)
            : throw new ArgumentException("The path does not contain a valid directory.", nameof(path));
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
}
