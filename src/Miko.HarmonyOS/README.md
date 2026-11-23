# Miko.HarmonyOS

HarmonyOS / OpenHarmony API 15+ host for Miko on .NET 11 NativeAOT. The native XComponent
owns an EGL ES 2 context; Skia draws directly into its window framebuffer. UI layout and
components use the same Miko engine as the other hosts.

The host provides density-aware touch input, primary-pointer tracking/cancellation, inertial
scrolling, back-button dispatch, soft-keyboard text/editing operations, resize handling,
foreground/background suspension, async dispatcher pumping and deterministic GL teardown.
Idle vsync callbacks do not render or swap buffers. The ArkUI shell uses the system's normal
safe content area, so it does not apply a second inset. Surface size changes update the viewport.
HarmonyOS selects Ionic's Material mode and mobile scrollbar defaults.

## Application integration

Reference `Miko.HarmonyOS` and export the NativeAOT initialization function from the app:

```csharp
[UnmanagedCallersOnly(EntryPoint = "openharmony_initialize", CallConvs = [typeof(CallConvCdecl)])]
public static nint Initialize() => MikoHarmonyApp.Initialize(() => App.CreateContext());
```

Use `Microsoft.NET.Sdk` with `TargetFramework=net11.0-ohos` and set
`OpenHarmonySurface=true` in the application. Install the released `harmonyos` workload
`11.0.0` into a standalone .NET SDK 11.0.100; see the example guides for setup.
The Framework package provides
`MediaPlayer`, `NativeImage`, `NativeWindow`, `EglContext` and the reusable `SurfaceView` host.
Its prebuilt native library and provider-owned ArkTS Stage shell are packaged automatically;
Miko contains only C# adapters. The `net11.0-ohos` workload also references Framework implicitly.
Applications no longer reference `Player.Sdk` or manually select NativeAOT/runtime properties.
`Miko.HarmonyOS` itself remains a `net11.0` adapter library with an explicit Framework `11.0.0`
dependency so its managed logic can be tested on the desktop. The workload TFM is for app heads;
the released SDK also generates application entry points and HAPs when that TFM is used.

All host callbacks, graphics work and media commands run on the UI thread. Framework drains its
platform dispatcher before Miko renders, including idle frames. It owns native callback and window
lifetimes; Miko wraps video textures and translates media events. The host owns the app context
and disposes it before releasing EGL. One surface/application per process is currently supported.

The example `build.ps1` scripts wrap standard `dotnet publish` and `dotnet run` commands.
Packages restore from NuGet; neither a sibling repository nor a local preview feed is required.
HAPs use the standard `bin/<configuration>/net11.0-ohos/<rid>/publish/` output directory.
See the [Framework documentation](https://github.com/chaldea/openharmony-net/tree/master/src/OpenHarmony.Framework)
for API and extension contracts.

## Dependencies and scope

The released HarmonyOS graphics packages `11.0.0` require **SkiaSharp 4.157.0**
and **HarfBuzzSharp 14.4.0.200**. Do not mix 4.158 managed bindings with the 157 native library.
Miko's shared dependency minimum matches 4.157; Browser and Desktop select their own matching
4.158 managed/native pairs. System HarmonyOS fonts are loaded from `/system/fonts`, not redistributed.
The host maps the engine's default and CSS generic font names to HarmonyOS Sans so whitespace
and line metrics use the same font as the visible glyphs. App-registered families take precedence.

This platform implements rendering, ordinary Ionic interaction, and the optional
`UseHarmonyOSVideo()` AVPlayer backend. Video frames flow through an `OH_NativeImage` external
texture acquired on the EGL render thread, leaving Miko's Skia player controls and overlays in
the same scene. Enable it when building the shared app context:

```csharp
builder.UseHarmonyOSVideo();
```

See the [Anime HarmonyOS example](../../examples/App/Anime/Anime.HarmonyOS/README.md) for a runnable
player. Local files and HTTP(S) sources use AVPlayer; its supported discrete playback speeds are
selected by nearest match. The current emulator retains the previous picture during a paused seek
and shows the target frame on resume. Framework exposes the complete texture matrix; the current Miko adapter handles the tested vertical flip, not arbitrary rotation/crop transforms. Camera capture and optional `Miko.Native` services are not implemented;
applications using `Miko.Native` can register its explicit unsupported defaults.
IME composition/preedit, clipboard actions, accessibility and hardware keyboard shortcuts are
not yet mapped. Navigation state is recreated if the OS destroys the surface/process.

See [ISSUE-170 validation](../../docs/ISSUE-170-validation.md) for device checks and the IME
verification limitation on the current emulator.
