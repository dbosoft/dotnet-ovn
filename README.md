# dotnet-ovn
Open Virtual Network .NET Agent and management libraries

## Description

This repository contains a .NET management library for [OVN](https://github.com/ovn-org/ovn).


**Features**:  
- OVS and OVN database management with ovn-nbctl and ovs-vsctl
- Single node and multiple node OVN hosting
- Chassis node initialization for OVN
- OVN configuration by network plan (API and command line tool)
- OVN cluster desired state configuration using plans (API and command line tool)

## Platforms & Prerequisites

**.NET**

The library requires .NET 8.0 or higher.

Supported platforms of core library: Windows and Linux.
Supported platforms for hosting: Windows

**OVN and OVS**

A Windows build of OVN and OVS is required. We maintain a fork of [OVN](https://github.com/dbosoft/ovn) that 
provides the required toolset to build on windows.

## OVN Agent cmdline tool

The OVN Agent cmdline can be used to run a basic OVN local setup. 
The OVN Agent will run all tools for OVN northbound and southbound and the OVS chassis. 

In addition the OVN Agent can apply a network plan from YAML files. It can also apply
a cluster or chassis plan from YAML files to configure a multi-node cluster.

## Integration tests
This repository contains integration tests which start ovsdb-server instances with
the different database schemas.

The tests currently only work on Windows and require that the aforementioned Windows
build of OVS and OVN is installed (the binaries must be present in the file system).
It is not necessary to install the Hyper-V switch extension and the tests do not make
changes to the networking settings. The integration tests only start a database process.

The integration tests create a working directory under `C:\ProgramData\dotnet-ovn-e2e`.

### Setup

1. Install the dbosoft OVS/OVN package (the one shipped in `ovspackage.zip`)
   under `C:\openvswitch\` so the layout matches:
   - `C:\openvswitch\usr\bin\` (ovn-* / ovs-* tools)
   - `C:\openvswitch\usr\sbin\` (ovsdb-server.exe, ovs-vswitchd.exe)
   - `C:\openvswitch\usr\share\openvswitch\` and `...\usr\share\ovn\` (schema files)
2. **Verify `pthreadVC3.dll` is present in BOTH `usr\bin\` and `usr\sbin\`.**
   The package ships it in both `bin/` and `sbin/`; if you only copy `bin/pthreadVC3.dll`,
   `ovsdb-server.exe` and `ovs-vswitchd.exe` will exit immediately with
   `STATUS_DLL_NOT_FOUND` (0xC0000135) and the test will hang forever in
   `WaitForDbSocket` because the spawned process dies before creating the listening socket.
3. The eryph services that share the same install root (`ovsdb-server`,
   `ovs-vswitchd`, `ovn-controller`) do not collide with the test instances
   because each test uses a random working directory and its own socket path,
   but make sure no production process is holding a lock on the install root
   if you are re-extracting the package.

### Schema versions

The expected NB / SB / OVS schema versions are hard-coded in the test base
classes (`OvnControlToolTestBase`, `OvnSouthboundControlToolTestBase`,
`OvsControlToolTestBase`). Bump them whenever the OVS / OVN package is updated.
Current expectations correspond to OVN 26.03 / OVS 3.7:

| Database       | Schema version |
|----------------|----------------|
| OVN_Northbound | 7.18.0         |
| OVN_Southbound | 21.8.0         |
| Open_vSwitch   | 8.8.0          |
