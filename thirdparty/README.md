# Third-Party Components

This directory contains third-party software distributed together with N2N-Hub.

The contents of this directory are **not part of the N2N-Hub project itself**. They are included to simplify building and deploying N2N-Hub and remain subject to their respective upstream licenses and copyright notices.

## n2n

N2N-Hub uses the upstream [n2n](https://github.com/ntop/n2n) project as its native VPN component.

The bundled n2n source is located at:

```text
thirdparty/n2n/
```

### Version

The currently bundled version is:

```text
3.1.1
```

The source should be kept as close to the upstream version as practical. Any local modifications should be clearly documented.

### Linux Build

The bundled n2n source has been tested on a Linux x86-64 environment using the upstream-style Autotools build process.

Install the required build tools first:

```bash
sudo apt update
sudo apt install build-essential autoconf automake libtool pkg-config
```

Then run:

```bash
cd thirdparty/n2n

./autogen.sh
./configure
make -j"$(nproc)"
```

A successful build produces the native N2N executables, including:

```text
edge
supernode
```

### Build Verification

The resulting Supernode can be checked with:

```bash
./supernode --help
```

A successful build should report the bundled N2N version.

The binary can also be inspected with:

```bash
file supernode
ldd supernode
```

The tested Linux x86-64 build produced a dynamically linked ELF executable using the system GNU C library.

A basic runtime test can be performed with:

```bash
./supernode -p 7654 -f
```

The Supernode is considered successfully started when it reports that its main service and management service are listening and eventually prints:

```text
supernode started
```

When running Supernode as an unprivileged user, the process may report an `Operation not permitted` message while attempting to drop privileges. This does not necessarily indicate that Supernode failed to start.

### License

n2n is an upstream third-party project and is **not part of the N2N-Hub source code**.

The n2n source included in this directory is governed by the license terms of the upstream n2n project. Please refer to the license and copyright notices included with:

```text
thirdparty/n2n/
```

before modifying or redistributing it.

For the upstream project and additional information, see:

https://github.com/ntop/n2n
