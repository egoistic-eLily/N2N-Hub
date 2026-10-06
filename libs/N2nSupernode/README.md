# N2nSupernode

`N2nSupernode` is a cross-platform .NET library for managing a native N2N `supernode` process.

It provides a simple interface for applications to start and stop a Supernode, receive its process messages, and communicate with the Supernode through its management interface.

The library does **not** implement the N2N protocol and does not contain the Supernode itself. The actual VPN functionality is provided by the native N2N `supernode` executable.

## Usage

A basic example:

```csharp
using N2nSupernode;

var options = new SupernodeOptions
{
    ExecutablePath = "thirdparty/bin/supernode",
    Port = 7654,
    ManagementPort = 5645,
    CommunityListPath = "Config/community.list"
};

SupernodeHost.MessageReceived += (_, message) =>
{
    Console.WriteLine(message.Text);
};

SupernodeHost.Exited += (_, code) =>
{
    Console.WriteLine($"supernode exited: {code}");
};

SupernodeHost.Start(options);

string reply = await SupernodeHost.SendManagementCommandAsync(
    "reload_communities"
);

SupernodeHost.Stop();
```

The exact paths and ports depend on the N2N deployment.

## Main API

`SupernodeHost` provides the main process-management interface.

### Process management

* `Start(options)` — starts the configured Supernode process.
* `Stop()` — stops the managed Supernode process.
* `Exited` — notifies the application when the process exits.

### Messages

`MessageReceived` provides messages generated during Supernode operation.

The message object contains:

| Property    | Description       |
| ----------- | ----------------- |
| `Timestamp` | Message timestamp |
| `Level`     | Message level     |
| `Source`    | Message source    |
| `Text`      | Message content   |

### Management interface

`SendManagementCommandAsync()` can be used to send commands supported by the native N2N Supernode management interface.

Common commands include:

| Command              | Purpose                               |
| -------------------- | ------------------------------------- |
| `reload_communities` | Reload the configured community list  |
| `communities`        | Query currently known communities     |
| `edges`              | Query currently connected edge nodes  |
| `stop`               | Request a graceful Supernode shutdown |

The available commands ultimately depend on the N2N Supernode version being used.

## Platform support

The library is designed to run on both **Windows and Linux**.

The application provides the path to the native Supernode executable through `SupernodeOptions.ExecutablePath`.

N2nSupernode does not include or compile the N2N Supernode itself. A compatible native N2N executable must be provided separately.

## Relationship with N2N

N2nSupernode is an independently developed component of N2N-Hub.

It communicates with the native N2N Supernode as an external process and does not implement or replace any part of the N2N VPN protocol.

The native N2N Supernode is a separate third-party component and remains subject to the license terms and copyright notices of the upstream [n2n](https://github.com/ntop/n2n) project.

## License

N2nSupernode is licensed under the **GNU General Public License v3.0 or later**.

Copyright © 2026 Tsukasa Sakuraochi.

You may redistribute and/or modify N2nSupernode under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

See the [LICENSE](LICENSE) file for the complete license text.
