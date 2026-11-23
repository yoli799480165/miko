#Requires -Version 7.3
[CmdletBinding()]
param(
    [string]$CommandLineTools = $env:OHOS_COMMAND_LINE_TOOLS,
    [string]$Device = '127.0.0.1:5555',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../../../../artifacts/anime-framework-routes')
)
& (Join-Path $PSScriptRoot '../../../Ionic/IonicDemo.HarmonyOS/verify.ps1') `
    -CommandLineTools $CommandLineTools -Device $Device -BundleName 'org.miko.anime' `
    -Routes @('/home','/trending','/timetable','/favorites','/history','/profile','/vip','/user') `
    -OutputDirectory $OutputDirectory
