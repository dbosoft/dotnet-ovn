# dotnet-ovn
Open Virtual Network .NET Agent and management libraries

## Description

This repository contains a .NET management library for [OVN](https://github.com/ovn-org/ovn).


**Features**:  
- OVS and OVN database management with ovn-nbctl and ovs-vsctl
- single node OVN hosting (no clusters supported currently)
- Chassis node initialization for OVN
- OVN configuration by network plan (api and cmdline tool)

## Platforms & Prerequisites

**.NET**

The library requires .NET 8.0 or higher.

Supported platforms of core library: Windows and Linux.
Supported platforms for hosting: Windows

**OVN and OVS**

A windows build of OVN and OVS is required. We maintain a fork of [OVN](https://github.com/dbosoft/ovn) that 
provides the required toolset to build on windows.



## OVN Agent cmdline tool

The OVN Agent cmdline can be used to run a basic OVN local setup. 
The OVN Agent will run all tools for OVN northbound and southbound and the OVS chassis. 

In addition the OVN Agent can apply a network plan from YAML files.

