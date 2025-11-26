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

The integration tests create a working directory under C:\ProgramData.
