# N2N-Hub

[![License: GPL-3.0-or-later](https://img.shields.io/badge/License-GPL--3.0--or--later-blue.svg)](LICENSE)

[![Language](https://img.shields.io/badge/Language-English-blue)](README.md) [![文档](https://img.shields.io/badge/文档-中文-red)](docs/README.zh-CN.md)

**N2N-Hub is a lightweight management platform for [n2n](https://github.com/ntop/n2n) VPN deployments.**

It provides a centralized way to manage N2N users and communities, maintain connection configuration, operate native N2N supernodes, and provide configuration information to client applications.

N2N-Hub does **not** implement the N2N VPN protocol itself. The actual VPN functionality is provided by the native N2N `supernode` and `edge` components. N2N-Hub focuses on management, configuration, administration, and service control.

## What this project provides

N2N-Hub is designed as a management hub for a native N2N deployment rather than as a replacement for N2N itself.

* **User management** — manage N2N users and their related account information.
* **Community management** — manage N2N communities and their configuration.
* **Configuration management** — centrally maintain the configuration required by N2N clients.
* **Web administration console** — provide a built-in web interface for administration and daily management.
* **Supernode management** — start, stop, restart, and otherwise manage the native N2N supernode service.
* **Community list management** — maintain the N2N community list used by the supernode.
* **Client configuration delivery** — provide clients with the information they need to establish an N2N connection.
* **HTTPS access** — provide secure web access to the management service.

## N2N dependency

N2N-Hub works together with the native [N2N](https://github.com/ntop/n2n) project and does not replace its VPN components.

The N2N `supernode` and `edge` programs remain responsible for the actual VPN networking. N2N-Hub manages the surrounding users, communities, configuration, administration, and supernode service.

## Deployment model

N2N-Hub is intended to run as the management service for an N2N deployment. The native N2N supernode is normally operated as part of the same deployment and can be managed by N2N-Hub.

Client applications can obtain the configuration they need from N2N-Hub and then use the native N2N `edge` component for the actual VPN connection.

## Client application

N2N-Hub is designed to work with a dedicated client application for end users.

The N2N-Hub client is currently under active development and is not yet included in this repository or the current release.

The client will provide a native user-facing interface for connecting to an N2N-Hub deployment and managing the local N2N connection.

The client is planned as a separate project and will be published when it reaches a suitable level of stability and completeness.

## Getting started

### Prerequisites

Before running N2N-Hub, prepare the following:

* **.NET 10** runtime/SDK suitable for the published build.
* **A domain name or host address** for the management service.
* **An HTTPS certificate** suitable for the configured address.
* **A compiled N2N supernode executable** compatible with your deployment.

The project creates its initial configuration and required data directories on first startup. Review and configure them before using the service in a real deployment.

### Run

For a development environment:

```bash
dotnet run
```

For a release build:

```bash
dotnet publish -c Release
```

After configuration, start N2N-Hub and open the built-in web administration console from your configured service address.

## License

N2N-Hub is licensed under the **GNU General Public License v3.0 or later**.

Copyright © 2026 Tsukasa Sakuraochi.

You may redistribute and/or modify N2N-Hub under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

See the [`LICENSE`](LICENSE) file for the complete license text.

### Third-party components

N2N-Hub uses and may include third-party components that are subject to their own license terms.

* **N2N** — N2N-Hub uses the native [n2n](https://github.com/ntop/n2n) project. N2N is a separate upstream project and remains subject to its original license, copyright notices, and license terms.
* **Other third-party components** — Any bundled third-party component remains subject to its applicable license and copyright notices.

Please review the corresponding license files and notices before redistributing bundled third-party components.
