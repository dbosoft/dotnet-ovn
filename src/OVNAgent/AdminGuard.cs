using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Dbosoft.OVNAgent;

public static partial class AdminGuard
{
    public static bool IsElevated()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return geteuid() == 0;

        var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    [LibraryImport("libc", EntryPoint = "geteuid", StringMarshalling = StringMarshalling.Utf8)]
    private static partial uint geteuid();
}
