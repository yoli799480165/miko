using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Miko.HarmonyOS;

namespace IonicDemo.HarmonyOS;

public static class Entry
{
    [UnmanagedCallersOnly(EntryPoint = "openharmony_initialize", CallConvs = [typeof(CallConvCdecl)])]
    public static nint Initialize() => MikoHarmonyApp.Initialize(() => App.CreateContext());
}
