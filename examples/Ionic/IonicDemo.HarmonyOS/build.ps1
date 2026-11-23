#Requires -Version 7.3
[CmdletBinding()]
param(
    [string]$CommandLineTools = $env:OHOS_COMMAND_LINE_TOOLS,
    [string]$JavaHome = $env:JAVA_HOME,
    [ValidateSet('x86_64','arm64-v8a')][string]$Abi = 'x86_64',
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [string]$Device = '',
    [string]$Dotnet = 'dotnet',
    [string]$SdkRoot = $env:OHOS_SDK_ROOT,
    [ValidateRange(0,2147483647)][int]$ApiLevel = 0,
    [string]$PackageCache = '',
    [string]$PowerShell = '',
    [switch]$Run
)
$ErrorActionPreference = 'Stop'
if (!$CommandLineTools -or !$JavaHome) { throw 'Set OHOS_COMMAND_LINE_TOOLS and JAVA_HOME, or pass -CommandLineTools and -JavaHome.' }
$CommandLineTools = (Resolve-Path -LiteralPath $CommandLineTools).Path
$JavaHome = (Resolve-Path -LiteralPath $JavaHome).Path
$project = Join-Path $PSScriptRoot 'IonicDemo.HarmonyOS.csproj'
$properties = @("-p:OpenHarmonyAbi=$Abi",
    "-p:OpenHarmonyCommandLineTools=$CommandLineTools", "-p:OpenHarmonyJavaHome=$JavaHome")
if ($SdkRoot) { $properties += "-p:OpenHarmonySdkRoot=$((Resolve-Path -LiteralPath $SdkRoot).Path)" }
if ($ApiLevel) { $properties += "-p:OpenHarmonyApiLevel=$ApiLevel" }
if ($Device) { $properties += "-p:OpenHarmonyDevice=$Device" }
if ($PackageCache) { $properties += "-p:RestorePackagesPath=$([IO.Path]::GetFullPath($PackageCache))" }
if ($PowerShell) { $properties += "-p:OpenHarmonyPowerShell=$PowerShell" }

# The installed harmonyos workload supplies the runtime, NativeAOT, Stage and HAP targets.
# Use the same standalone SDK for workload installation and this command (-Dotnet or PATH).
if ($Run) {
    & $Dotnet run --project $project -c $Configuration --no-launch-profile @properties '-p:OpenHarmonyUseDevelopmentSigning=true'
} else {
    & $Dotnet publish $project -c $Configuration @properties
}
if ($LASTEXITCODE -ne 0) { throw "$Dotnet failed with exit code $LASTEXITCODE" }
