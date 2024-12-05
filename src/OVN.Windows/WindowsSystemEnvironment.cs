using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Dbosoft.OVN.Windows;

public class WindowsSystemEnvironment(
    ILoggerFactory loggerFactory)
    : SystemEnvironment(loggerFactory)
{
    public override IOvsExtensionManager GetOvsExtensionManager() =>
        new WindowsOvsExtensionManager("dbosoft Open vSwitch Extension");

    public override IServiceManager GetServiceManager(
        string serviceName) =>
        new WindowsServiceManager(serviceName, this);
}
