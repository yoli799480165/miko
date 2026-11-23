using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Platform.Video;
using Shouldly;

namespace Miko.HarmonyOS.Tests;

public sealed class HarmonyOSVideoBackendTests
{
    [Fact]
    public void Should_Register_System_Backend_With_Local_And_Network_Media_Capabilities()
    {
        using var app = MikoAppBuilder.CreateDefault()
            .UseHarmonyOSVideo()
            .Build();

        var backend = app.Services.GetRequiredService<IVideoBackend>();
        backend.ShouldBeOfType<HarmonyOSVideoBackend>();
        backend.Capabilities.HardwareDecode.ShouldBeTrue();
        backend.Capabilities.SupportedMimeTypes.ShouldContain("video/mp4");
        backend.Capabilities.SupportedMimeTypes.ShouldContain("application/vnd.apple.mpegurl");
    }

    [Fact]
    public void Should_Use_Framework_Playback_Rate_Policy()
        => OpenHarmony.Media.PlaybackRate.GetSupportedRate(1.2f).ShouldBe(1.25f);
}
