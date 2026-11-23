#Requires -Version 7.3
[CmdletBinding()]
param(
    [string]$CommandLineTools = $env:OHOS_COMMAND_LINE_TOOLS,
    [string]$Device = '127.0.0.1:5555',
    [string]$BundleName = 'org.miko.ionicdemo',
    [string[]]$Routes = @(),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../../../artifacts/issue170-device')
)
$ErrorActionPreference = 'Stop'
$hdc = Join-Path $CommandLineTools 'sdk/default/openharmony/toolchains/hdc.exe'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$OutputDirectory = (Resolve-Path $OutputDirectory).Path
function Hdc([string[]]$Arguments) {
    $result = & $hdc -t $Device @Arguments
    if ($LASTEXITCODE -ne 0) { throw "HDC failed: $Arguments" }
    return ($result | Out-String).Trim()
}
$appPid = Hdc @('shell','pidof',$BundleName)
if ($appPid -notmatch '^\d+$') { throw 'Start IonicDemo with build.ps1 -Run first.' }
if (!$Routes.Count) {
    $Routes = @(Get-ChildItem (Join-Path $PSScriptRoot '../IonicDemo/Pages') -Filter '*.razor' |
        Select-String '^@page "([^"]+)"' | ForEach-Object { $_.Matches[0].Groups[1].Value } | Sort-Object -Unique)
}
$results = @()
foreach ($route in $Routes) {
    $start = Hdc @('shell','aa','start','-a','EntryAbility','-b',$BundleName,'--ps','openharmonyRoute',$route)
    if ($start -notmatch 'success') { throw "Cannot open $route : $start" }
    $deadline = (Get-Date).AddSeconds(10)
    do {
        Start-Sleep -Milliseconds 250
        $log = Hdc @('shell','hilog','-x','-P',$appPid,'-T','OpenHarmony')
        if ($log -match 'OH_FRAMEWORK_ERROR|OH_EGL_ERROR|OH_XCOMPONENT_ERROR') { throw "Renderer failed: $log" }
        $rendered = [regex]::Matches($log, 'rendered route=(\S+) pixels=')
        if ($rendered.Count -gt 0 -and $rendered[-1].Groups[1].Value -eq $route) { break }
    } while ((Get-Date) -lt $deadline)
    if (!$rendered.Count -or $rendered[-1].Groups[1].Value -ne $route) { throw "No rendered frame for $route" }
    # Let navigation transitions and asynchronous embedded images settle before capturing.
    Start-Sleep -Milliseconds 450
    $name = if ($route -eq '/') { 'home' } else { $route.Trim('/').Replace('/','-') }
    $remote = '/data/local/tmp/miko-issue170.jpeg'
    Hdc @('shell','snapshot_display','-f',$remote) | Out-Null
    Hdc @('file','recv',$remote,(Join-Path $OutputDirectory "$name.jpeg")) | Out-Null
    $results += @{ route=$route; pid=$appPid; rendered=$true; screenshot="$name.jpeg" }
    Write-Host "Rendered $route"
}
Hdc @('shell','hilog','-x','-P',$appPid) | Set-Content -LiteralPath (Join-Path $OutputDirectory 'hilog.log') -Encoding utf8
if ((Hdc @('shell','pidof',$BundleName)) -ne $appPid) { throw 'Application exited or restarted during verification.' }
$results | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'routes.json') -Encoding utf8
Write-Host "Verified $($results.Count) route renders in process $appPid. Screenshots: $OutputDirectory"
