using System.Management;
using System.Runtime.Versioning;

namespace Dbosoft.OVN;

[SupportedOSPlatform("windows")]
public class WMIOvsExtensionManager : IOvsExtensionManager
{
    private readonly string _extensionName;
    private object _syncRoot = new();
    public WMIOvsExtensionManager(string extensionName)
    {
        _extensionName = extensionName;
    }
    
    public bool IsExtensionEnabled()
    {
        try
        {
            lock (_syncRoot)
            {
                using var extensionsSearcher = new ManagementObjectSearcher(@"\\.\root\virtualization\v2",
                    "SELECT Name FROM Msvm_EthernetSwitchExtension where " +
                    $"ElementName=\"{_extensionName}\" AND EnabledState=2 AND HealthState=5");

                using var extensions = extensionsSearcher.Get();

                if (extensions.Count >= 1)
                    return true;
            }
        }
        catch (Exception)
        {
            // ignored
        }
        
        return false;
    }
    
}