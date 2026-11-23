# Anime on HarmonyOS

This head runs the shared Anime app with the C# Miko adapter and the Framework's AVPlayer
backend. It targets `net11.0-ohos` with the published HarmonyOS workload `11.0.0` and
graphics native packages `11.0.0`. The minimum platform is API 15.

Install the workload into a standalone .NET SDK 11.0.100 and configure
`OHOS_COMMAND_LINE_TOOLS` and `JAVA_HOME` as described in the
[IonicDemo setup guide](../../../Ionic/IonicDemo.HarmonyOS/README.md#setup).
Neither a local preview feed nor a sibling `harmonyos-net` checkout is needed.

From this directory:

```powershell
dotnet build
dotnet run
dotnet publish -c Release
dotnet publish -c Release -p:OpenHarmonyAbi=arm64-v8a
```

The default ABI is x86_64 for the emulator. `dotnet run` uses development signing for
Debug; Release runs need a signed HAP or `-p:OpenHarmonyUseDevelopmentSigning=true`.
Outputs follow the standard path `bin/<configuration>/net11.0-ohos/<rid>/publish/`.
The workload automatically selects the runtime, NativeAOT settings, Framework and Stage shell.
The app exports `openharmony_initialize`; route injection uses `openharmonyRoute`.

The optional PowerShell 7.3+ wrapper provides the same publish/run flow:

```powershell
pwsh ./build.ps1 -Run
pwsh ./build.ps1 -Abi arm64-v8a
```

Use `-Dotnet <standalone-dotnet>` when that SDK is not first on PATH. Other options include
`-CommandLineTools`, `-JavaHome`, `-Configuration`, `-Device`, `-SdkRoot`, `-ApiLevel`,
`-PackageCache` and `-PowerShell`. `-Run` defaults to Release with explicit development signing.
The former `-HarmonyOSRepo` and `-SkipPublish` options have been removed; publishing is incremental.

Run `pwsh ./verify.ps1 -CommandLineTools $env:OHOS_COMMAND_LINE_TOOLS` after installing to
verify the eight non-detail routes and save screenshots. Select a poster on the home page
to exercise the video player. Playback/control acceptance and current limitations are recorded
in [ISSUE-170 validation](../../../../docs/ISSUE-170-validation.md).
