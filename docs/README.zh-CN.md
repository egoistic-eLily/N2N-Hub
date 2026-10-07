# N2N-Hub


[![License: GPL-3.0-or-later](https://img.shields.io/badge/License-GPL--3.0--or--later-blue.svg)](LICENSE)
**N2N-Hub 是一个面向 [n2n](https://github.com/ntop/n2n) VPN 部署的轻量级管理平台。**

它提供集中化的 N2N 用户与社区管理、连接配置管理、原生 N2N Supernode 服务管理，以及向客户端提供连接所需配置信息的能力。

N2N-Hub **本身不实现 N2N VPN 协议**。实际的 VPN 网络功能仍由原生 N2N 的 `supernode` 和 `edge` 组件负责，N2N-Hub 主要负责管理、配置、后台管理以及服务控制。

## 项目提供的功能

N2N-Hub 的定位是一个原生 N2N 部署的管理中心，而不是用于替代 N2N 本身。

* **用户管理** — 管理 N2N 用户及其相关账户信息。
* **社区管理** — 管理 N2N Community 以及相关配置。
* **配置管理** — 集中维护 N2N 客户端所需的连接配置。
* **Web 管理控制台** — 提供内置的 Web 管理界面，用于日常管理和维护。
* **Supernode 管理** — 对原生 N2N Supernode 服务进行启动、停止、重启等管理操作。
* **Community 列表管理** — 维护 Supernode 使用的 N2N Community 列表。
* **客户端配置下发** — 向客户端提供建立 N2N 连接所需的信息。
* **HTTPS 访问** — 为管理服务提供安全的 HTTPS 访问方式。

## N2N 依赖

N2N-Hub 与原生 [N2N](https://github.com/ntop/n2n) 项目配合工作，并不会取代 N2N 的 VPN 组件。

实际的 VPN 网络通信仍由 N2N 的 Supernode 与 Edge 程序负责。N2N-Hub 负责外围的用户、社区、配置、后台管理以及 Supernode 服务管理。

## 部署方式

N2N-Hub 用于作为 N2N 部署中的管理服务运行。原生 N2N Supernode 通常作为同一套部署的一部分运行，并可以由 N2N-Hub 进行管理。

客户端可以从 N2N-Hub 获取建立连接所需的配置信息，然后使用原生 N2N Edge 组件完成实际的 VPN 连接。

## 客户端

N2N-Hub 计划配套一个独立的客户端应用，供最终用户使用。

目前客户端仍在开发中，尚未达到公开发布的状态，因此**不会包含在当前仓库或当前版本的发布包中**。

客户端计划提供面向普通用户的原生操作界面，用于连接 N2N-Hub 部署并管理本地 N2N 网络连接。

客户端将作为独立项目开发，并将在达到合适的稳定性和完整度后正式公开。


## 快速开始

### 运行环境

运行 N2N-Hub 前，请准备：

* **.NET 10** 运行环境或 SDK，具体取决于你的发布方式。
* **域名或主机地址**，用于访问管理服务。
* **HTTPS 证书**，并与配置的访问地址匹配。
* **已经编译好的 Nw2N Supernode 可执行文件**，并与部署环境兼容。

N2N-Hub 首次启动时会创建初始配置以及所需的数据目录。正式使用前，请先检查并完成相关配置。

### 运行

开发环境可以直接使用：

```bash
dotnet run
```

发布 Release 版本：

```bash
dotnet publish -c Release
```

完成配置后启动 N2N-Hub，然后通过配置好的服务地址进入内置的 Web 管理控制台。


## 许可证

N2N-Hub 使用 **GNU General Public License v3.0 or later（GNU GPL v3.0 或更高版本）**授权。

Copyright © 2026 Tsukasa Sakuraochi.

你可以根据自由软件基金会发布的 GNU General Public License 条款重新发布和/或修改 N2N-Hub，可以选择 GPL 第 3 版，或者（由你选择）任何更高版本。

完整的许可证文本请参阅 [`LICENSE`](LICENSE) 文件。

### 第三方组件

N2N-Hub 使用并可能附带受其自身许可证条款约束的第三方组件。

* **N2N** — N2N-Hub 使用原生的 [n2n](https://github.com/ntop/n2n) 项目。N2N 是一个独立的上游项目，仍然遵循其原有的许可证、版权声明及相关许可条款。
* **其他第三方组件** — 任何附带的第三方组件仍然遵循其适用的许可证及版权声明。

在重新发布附带的第三方组件或将其包含在其他项目中之前，请仔细阅读相应的许可证文件和版权声明。
