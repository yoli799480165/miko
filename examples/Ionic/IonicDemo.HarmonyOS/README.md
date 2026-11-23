# IonicDemo on HarmonyOS

This head targets `net11.0-ohos` using `Microsoft.NET.Sdk` and the released HarmonyOS
workload `11.0.0`. The workload supplies Framework, NativeAOT, the runtime and HAP tooling;
the app references the released SkiaSharp/HarfBuzzSharp HarmonyOS native packages `11.0.0`.
No local `harmonyos-net` checkout, preview feed or `Player.Sdk` reference is required.

## Setup

Use a standalone .NET SDK 11.0.100 (ZIP/tarball installation) on PATH. The workload's
file-based packs cannot be installed into an MSI/Visual Studio-managed SDK. Use the same
SDK for installation and all application commands. Install the official HarmonyOS CLI
tools/Native SDK and JDK 17+, then configure their paths:

```powershell
$env:DOTNET_ROOT = 'C:\dotnet11'
$env:PATH = "$env:DOTNET_ROOT;$env:USERPROFILE\.dotnet\tools;$env:PATH"
$env:OHOS_COMMAND_LINE_TOOLS = 'F:\.huawei\command-line-tools'
$env:JAVA_HOME = 'D:\Program Files\Huawei\DevEco Studio\jbr'

dotnet tool install -g Chaldea.OpenHarmony.Workload --version 11.0.0
harmonyos-workload init
dotnet workload install harmonyos --skip-manifest-update
dotnet workload list
```

If the bootstrap tool is already installed, use `dotnet tool update -g
Chaldea.OpenHarmony.Workload --version 11.0.0`. For an existing workload registration,
follow the [workload upgrade instructions](https://github.com/chaldea/openharmony-net).
Confirm that `dotnet workload list` shows HarmonyOS `11.0.0`.

## Build and run

From this directory, standard commands are sufficient:

```powershell
dotnet build
dotnet run
dotnet publish -c Release
dotnet publish -c Release -p:OpenHarmonyAbi=arm64-v8a
```

`build` compiles managed code and validates the Native SDK. `publish` builds NativeAOT and
an unsigned HAP. `run` publishes, development-signs Debug builds, installs and starts the
app on the connected emulator. x86_64 is the default; use `OpenHarmonyAbi=arm64-v8a` for
devices and `OpenHarmonyDevice=<hdc-target>` when multiple devices are connected.
Release runs require your signed HAP or an explicit `OpenHarmonyUseDevelopmentSigning=true`.

HAPs are written to `bin/<configuration>/net11.0-ohos/<rid>/publish/`, with RID
`linux-musl-x64` or `linux-musl-arm64`. API 15 is the minimum platform; the compiler SDK
version is inferred from the installed CLI. Use `OpenHarmonySdkRoot` and
`OpenHarmonyApiLevel` if selecting a separate Native SDK.

The optional PowerShell 7.3+ wrapper accepts CLI/JDK paths, ABI, configuration, SDK root,
API level, device and `-Dotnet <standalone-dotnet>`. It invokes the same standard commands:

```powershell
pwsh ./build.ps1
pwsh ./build.ps1 -Run
pwsh ./build.ps1 -Abi arm64-v8a
```

The wrapper defaults to Release and `-Run` explicitly enables local development signing.
It no longer accepts `-HarmonyOSRepo` or `-SkipPublish`; normal publishing is incremental.
`-PowerShell <executable>` can override the workload's packaging shell when needed.

## Verification

The app exports `openharmony_initialize` and uses the provider-owned SurfaceView/Stage host.
Miko contains only C# adapters. Open a route through its ability Want:

```powershell
hdc shell aa start -a EntryAbility -b org.miko.ionicdemo --ps openharmonyRoute /checkbox
pwsh ./verify.ps1 -CommandLineTools $env:OHOS_COMMAND_LINE_TOOLS
```

The `OpenHarmony` log tag reports `OH_FRAMEWORK created runtime=11 backend=OpenGL`,
`first_frame=1` and `MIKO_HARMONY rendered route=...`. A host failure reports
`OH_FRAMEWORK_ERROR`. The verifier visits every shared route and records screenshots
and logs under `artifacts/issue170-device`; it checks page rendering, not all interactions.
See [the validation record](../../../docs/ISSUE-170-validation.md) for acceptance details.
