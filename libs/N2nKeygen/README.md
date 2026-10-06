# N2nKeygen

`N2nKeygen` is a C# library that provides N2N user public-key generation.

The implementation is a cross-language port of the key derivation functionality used by the upstream [n2n](https://github.com/ntop/n2n) project, specifically its `tools/n2n-keygen.c`.

This project is included in **N2N-Hub** as a library dependency and is used to generate the public key associated with an N2N username and password.

## Usage

Reference this project and call:

```csharp
using N2nKeygen.Core;

string publicKey = N2nUserKey.Generate(username, password);
```

The returned value is the N2N user public key used by the corresponding N2N configuration.

## Relationship with N2N-Hub

`N2nKeygen` is a separate library project included in the N2N-Hub repository.

The library is independently implemented in C#, but its key-generation logic is a cross-language port of the corresponding functionality from the upstream [n2n](https://github.com/ntop/n2n) project, specifically `tools/n2n-keygen.c`.

As a derivative work based on functionality from the upstream N2N project, `N2nKeygen` remains subject to the applicable license terms of the original N2N implementation.

N2N-Hub uses this library as a dependency to provide N2N user public-key generation.

## License

`N2nKeygen` is licensed under the **GNU General Public License v3.0 or later**.

Copyright © 2026 Tsukasa Sakuraochi.

This project is based on the corresponding key-generation functionality of the upstream [n2n](https://github.com/ntop/n2n) project. The original N2N implementation is copyrighted by its respective authors and contributors and is distributed under the GNU General Public License v3 or later.

See the [LICENSE](LICENSE) file for the complete license text.

Please retain the applicable copyright notices and license information from the upstream N2N project when redistributing or modifying this project.
