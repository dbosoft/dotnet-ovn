# Setup with two hosts
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
5. Start the dotnet-ovn agent: `OVNAgent.exe run --nodes AllInOne`
6. Create a bridge for the tunnel: `ovs-vsctl.exe -- add-br br-extern -- add-port br-extern eth0`
7. Assign an IP address: `Enable-NetAdapter -Name br-extern; New-NetIPAddress -InterfaceAlias br-extern -IPAddress 192.168.240.101 -PrefixLength 24`
8. Create a VM `vm-primary`
9. Set the port name of the VM: `OVSAgent.exe hyperv portname set {adapterId} ovs_vm-primary`
10. Add the VM port to the integration bridge: `ovs-vsctl.exe -- add-port br-int ovs_vm-primary -- set interface ovs_vm-primary external_ids:iface-id=ovs_vm-primary`

## Manual setup of `chassis-secondary`
1. Install Hyper-V
2. Extract the OVS/OVN package to `C:\openvswitch\user`
3. Install the OVS driver: `netcfg.exe /l 'C:\openvswitch\usr\driver\dbo_ovse.inf' /c s /i DBO_OVSE`
4. Create the overlay switch: `New-VMSwitch -Name ovs_overlay -NetAdapterName eth0 -AllowManagementOS $false | Enable-VMSwitchExtension -Name 'dbosoft Open vSwitch Extension'`
5. Start the dotnet-ovn agent: `OVNAgent.exe run --nodes Chassis`
6. Create a bridge for the tunnel: `ovs-vsctl.exe -- add-br br-extern -- add-port br-extern eth0`
7. Assign an IP address: `Enable-NetAdapter -Name br-extern; New-NetIPAddress -InterfaceAlias br-extern -IPAddress 192.168.240.102 -PrefixLength 24`
8. Create a VM `vm-secondary`
9. Set the port name of the VM: `OVSAgent.exe hyperv portname set {adapterId} ovs_vm-secondary`
10. Add the VM port to the integration bridge: `ovs-vsctl.exe -- add-port br-int ovs_vm-secondary -- set interface ovs_vm-secondary external_ids:iface-id=ovs_vm-secondary`

## Configuration of the cluster
1. Apply the chassis plan on primary: `OVSAgent.exe chassisplan apply --file ".\chassisplan_primary.yaml"`
2. Apply the chassis plan on secondary: `OVSAgent.exe chassisplan apply --file ".\chassisplan_secondary.yaml"`
3. Apply the cluster plan on primary: `OVSAgent.exe clusterplan apply --file ".\clusterplan.yaml"`
4. Apply the network plan on primary: `OVSAgent.exe netplan apply --file "D:\git\dotnet-ovn\samples\two-hosts\netplan.yaml"`
