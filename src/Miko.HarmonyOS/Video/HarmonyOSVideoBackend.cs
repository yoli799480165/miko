using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Platform.Video;

namespace Miko.HarmonyOS;

/// <summary>Registers HarmonyOS AVPlayer video playback and external-texture output.</summary>
public static class HarmonyOSVideoServiceExtensions
{
    /// <summary>Uses the HarmonyOS system AVPlayer with an EGL NativeImage output surface.</summary>
    public static MikoAppBuilder UseHarmonyOSVideo(this MikoAppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSingleton<IVideoBackend, HarmonyOSVideoBackend>();
        return builder;
    }
}

/// <summary>System AVPlayer backend for local files and HTTP(S) sources.</summary>
public sealed class HarmonyOSVideoBackend : IVideoBackend
{
    public VideoBackendCapabilities Capabilities { get; } = new(
        HardwareDecode: true,
        Hdr: false,
        SupportedMimeTypes: ["video/mp4", "video/3gpp", "video/webm", "video/x-matroska", "application/vnd.apple.mpegurl", "application/x-mpegURL"]);

    public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options)
        => new HarmonyOSVideoSession(source, options);
}
