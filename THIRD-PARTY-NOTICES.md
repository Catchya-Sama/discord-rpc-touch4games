# Third-party notices

This file lists material third-party runtime, development, and packaging dependencies used by TOUCH RPC. It does not grant a license for the TOUCH RPC source code; the project owner has not selected a project license yet.

## Runtime dependency

- **DiscordRichPresence 1.6.1.70**
  - Author: Lachee
  - License: MIT
  - Project: `https://github.com/Lachee/discord-rpc-csharp`
  - Used for local Discord Rich Presence IPC.

- **Microsoft .NET 10 runtime (win-x64)**
  - Author: .NET Foundation and contributors
  - License: MIT; the runtime package also includes its own third-party notices.
  - Project: `https://github.com/dotnet/runtime`
  - Bundled into the self-contained single-file executable so end users do not need to install .NET separately.

## Development and test dependency

- **MSTest 4.0.2**
  - Author: Microsoft
  - License: MIT
  - Project: `https://github.com/microsoft/testfx`
  - Used only to build and run automated tests; it is not required by the published desktop application.

- **WiX Toolset CLI 5.0.2**
  - Author: WiX Toolset Team
  - License: Microsoft Reciprocal License (MS-RL)
  - Project: `https://wixtoolset.org/`
  - Used to build the MSI; the WiX CLI itself is not distributed as part of TOUCH RPC.

The applicable full license texts and transitive dependency notices are available in the corresponding SDK, NuGet, and tool packages used to build this software. Review and refresh this file before a public release whenever dependency or SDK versions change.