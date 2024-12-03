using System.Management;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Windows;

internal class WindowsOvsExtensionManager(
    string extensionName)
    : IOvsExtensionManager
{
    public EitherAsync<Error, bool> IsExtensionEnabled() =>
        TryAsync(Task.Run(() =>
        {
            using var extensionsSearcher = new ManagementObjectSearcher(
                new ManagementScope(@"root\virtualization\v2"),
                new ObjectQuery("SELECT Name "
                                + "FROM Msvm_EthernetSwitchExtension "
                                + $"WHERE ElementName='{extensionName}' AND EnabledState=2 AND HealthState=5"));

            using var extensionsCollection = extensionsSearcher.Get();
            var extensions = extensionsCollection.Cast<ManagementBaseObject>().ToList();
            try
            {
                return extensions.Count >= 1;
            }
            finally
            {
                DisposeAll(extensions);
            }
        })).ToEither();

    private static void DisposeAll(
        IList<ManagementBaseObject> managementObjects)
    {
        foreach (var managementObject in managementObjects)
        {
            managementObject.Dispose();
        }
    }
}
