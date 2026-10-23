<#
.SYNOPSIS
  Builds the Microsoft Store (MSIX) package of PartitionExplorer.

.DESCRIPTION
  Publishes the app self-contained for win-x64, so Store users don't need the
  .NET 8 Desktop Runtime, lays it out with AppxManifest.xml and the Store
  logos, and packs it with makeappx. makeappx and makepri come from the
  Microsoft.Windows.SDK.BuildTools NuGet package, so no Windows SDK install
  is needed.

  The Store signs the package itself, so upload packaging\out\*.msix unsigned.

.PARAMETER IdentityName
  Package/Identity/Name from Partner Center.
.PARAMETER Publisher
  Package/Identity/Publisher from Partner Center, e.g. "CN=1A2B3C4D-...".
.PARAMETER PublisherDisplayName
  Package/Properties/PublisherDisplayName from Partner Center.
.PARAMETER Register
  Also installs the unpacked layout for testing (Add-AppxPackage -Register).
  This needs Developer Mode (Settings > System > For developers).

.EXAMPLE
  .\packaging\build-msix.ps1 -Register
  .\packaging\build-msix.ps1 -IdentityName 12345Kaveh.PartitionExplorer -Publisher "CN=..." -PublisherDisplayName Kaveh
#>
param(
    [string]$IdentityName = "PartitionExplorer.Dev",
    [string]$Publisher = "CN=PartitionExplorerDev",
    [string]$PublisherDisplayName = "PartitionExplorer (dev)",
    [string]$SdkBuildToolsVersion = "10.0.26100.1742",
    [switch]$Register
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$pkgDir = $PSScriptRoot
$obj = Join-Path $pkgDir "obj"
$layout = Join-Path $obj "layout"
$out = Join-Path $pkgDir "out"

# The package version follows the assembly version; the Store needs the last part to be 0
$assemblyInfo = Get-Content (Join-Path $root "PartitionExplorer\Properties\AssemblyInfo.cs") -Raw
if ($assemblyInfo -notmatch 'AssemblyFileVersion\("(\d+)\.(\d+)\.(\d+)\.\d+"\)') { throw "AssemblyFileVersion not found" }
$version = "$($Matches[1]).$($Matches[2]).$($Matches[3]).0"

# makeappx and makepri, from NuGet
$tools = Join-Path $obj "sdk-buildtools.$SdkBuildToolsVersion"
if (-not (Test-Path $tools)) {
    $nupkg = "$tools.zip"
    New-Item -ItemType Directory -Force $obj | Out-Null
    Invoke-WebRequest "https://www.nuget.org/api/v2/package/Microsoft.Windows.SDK.BuildTools/$SdkBuildToolsVersion" -OutFile $nupkg
    Expand-Archive $nupkg $tools
    Remove-Item $nupkg
}
$bin = Get-ChildItem (Join-Path $tools "bin") -Directory | Sort-Object Name -Descending | Select-Object -First 1
$makeappx = Join-Path $bin.FullName "x64\makeappx.exe"
$makepri = Join-Path $bin.FullName "x64\makepri.exe"

# App files
if (Test-Path $layout) { Remove-Item $layout -Recurse -Force }
dotnet publish (Join-Path $root "PartitionExplorer\PartitionExplorer.csproj") -c Release -r win-x64 --self-contained true -o $layout
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
Get-ChildItem $layout -Filter *.pdb | Remove-Item

# Manifest and logos
$manifest = (Get-Content (Join-Path $pkgDir "AppxManifest.xml") -Raw).
    Replace('$IdentityName$', $IdentityName).
    Replace('$Publisher$', [Security.SecurityElement]::Escape($Publisher)).
    Replace('$PublisherDisplayName$', [Security.SecurityElement]::Escape($PublisherDisplayName)).
    Replace('$Version$', $version)
Set-Content (Join-Path $layout "AppxManifest.xml") $manifest -Encoding UTF8
Copy-Item (Join-Path $pkgDir "Assets") (Join-Path $layout "Assets") -Recurse

# resources.pri lets Windows pick the right logo for each scale and taskbar size
$priConfig = Join-Path $obj "priconfig.xml"
& $makepri createconfig /cf $priConfig /dq en-US /o | Out-Null
& $makepri new /pr $layout /cf $priConfig /mn (Join-Path $layout "AppxManifest.xml") /of (Join-Path $layout "resources.pri") /o
if ($LASTEXITCODE -ne 0) { throw "makepri failed" }

New-Item -ItemType Directory -Force $out | Out-Null
$msix = Join-Path $out "PartitionExplorer_$($version)_x64.msix"
& $makeappx pack /d $layout /p $msix /o
if ($LASTEXITCODE -ne 0) { throw "makeappx failed" }
Write-Host "Built $msix"

if ($Register) {
    Add-AppxPackage -Register (Join-Path $layout "AppxManifest.xml") -ForceApplicationShutdown
    Write-Host "Installed the test layout. Find PartitionExplorer in the Start menu."
}
