# Two host example
This examples demonstrates the configuration of a two-host cluster with dotnet-ovn.

The setup consists of two Hyper-V hosts/chassis called `chassis-primary` and `chassis-secondary`:
- `chassis-primary` runs a full OVS and OVN setup
- `chassis-secondary` only runs OVS and the ovn controller

`chassis-secondary` connects to the southbound database on `chassis-primary`.

The configuration of the cluster and and the projects happen on `chassis-primary`
as it contains the northbound database.

## Requirements
The following is required for the example:
- two Hyper-V hosts (`chassis-priamry` and `chassis-secondary`). The hosts can be physical or virtual machines.
- each host has a network adapter called `eth0` (rename the adapter if necessary)
- the two hosts are connected via a switch

## Manual setup of `chassis-primary`
1. Install Hyper-V
2. Extract the OVS/OVN package to `C:\openvswitch\user`
3. Install the OVS driver: `netcfg.exe /l 'C:\openvswitch\usr\driver\dbo_ovse.inf' /c s /i DBO_OVSE`
4. Create the overlay switch: `New-VMSwitch -Name ovs_overlay -NetAdapterName eth0 -AllowManagementOS $false | Enable-VMSwitchExtension -Name 'dbosoft Open vSwitch Extension'`
5. Start the dotnet-ovn agent: `OVNAgent.exe run ...`
6. Create a bridge for the tunnel: `ovs-vsctl.exe -- add-br br-tunnel -- add-port br-tunnel eth0`
7. Assign an IP address: `Enable-NetAdapter -Name br-tunnel; New-NetIPAddress -InterfaceAlias br-tunnel -IPAddress 192.168.240.101 -PrefixLength 24`

## Manual setup of `chassis-secondary`
1. Install Hyper-V
2. Extract the OVS/OVN package to `C:\openvswitch\user`
3. Install the OVS driver: `netcfg.exe /l 'C:\openvswitch\usr\driver\dbo_ovse.inf' /c s /i DBO_OVSE`
4. Create the overlay switch: `New-VMSwitch -Name ovs_overlay -NetAdapterName eth0 -AllowManagementOS $false | Enable-VMSwitchExtension -Name 'dbosoft Open vSwitch Extension'`
5. Start the dotnet-ovn agent: `OVNAgent.exe run ...`
6. Create a bridge for the tunnel: `ovs-vsctl.exe -- add-br br-tunnel -- add-port br-tunnel eth0`
7. Assign an IP address: `Enable-NetAdapter -Name br-tunnel; New-NetIPAddress -InterfaceAlias br-tunnel -IPAddress 192.168.240.102 -PrefixLength 24`
