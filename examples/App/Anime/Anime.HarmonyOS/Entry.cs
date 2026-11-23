using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Miko.HarmonyOS;

namespace Anime.HarmonyOS;

public static class Entry
{
    [UnmanagedCallersOnly(EntryPoint = "openharmony_initialize", CallConvs = [typeof(CallConvCdecl)])]
    public static nint Initialize() => MikoHarmonyApp.Initialize(() => global::Anime.App.CreateContext(builder =>
    {
        builder.UseHarmonyOSVideo();
    }));
}
