[CmdletBinding()]
param([string]$XboxFrameworkProject)
$ErrorActionPreference = 'Stop'
if (-not $XboxFrameworkProject) {
    $XboxFrameworkProject = Join-Path $PSScriptRoot '../osu-xbox-port/osu-framework/osu.Framework/osu.Framework.csproj'
}
if (-not (Test-Path -LiteralPath $XboxFrameworkProject -PathType Leaf)) {
    throw @"
The Xbox-specific osu!framework source is missing:
$XboxFrameworkProject
The public upstream repository references this project but does not include it.
An unmodified ppy/osu-framework checkout cannot replace its UWP bridge and renderer.
Obtain the author's matching fork, then pass -XboxFrameworkProject <path>.
The downloaded v0.1.0 release can be installed independently of a source build.
"@
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Install the .NET 10 SDK or put a local SDK on PATH.'
}
$sdkVersions = & dotnet --list-sdks
if (-not ($sdkVersions -match '^10\.')) { throw 'The .NET 10 SDK is required.' }
Write-Output 'Source dependency and .NET SDK found. Publish command:'
Write-Output "dotnet publish uwp-osu/UwpOsu.csproj -c Release -r win-x64 -p:XboxPort=true -p:XboxFrameworkProject=`"$XboxFrameworkProject`""
Write-Output 'NativeAOT also requires Visual Studio C++ tools and Windows SDK 26100. Packaging scripts are absent upstream.'
