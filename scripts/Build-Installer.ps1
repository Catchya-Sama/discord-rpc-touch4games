param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$publishScript = Join-Path $PSScriptRoot "Publish-Release.ps1"
$publishDirectory = Join-Path $root "artifacts\TouchRpc-$Runtime"
$installerDirectory = Join-Path $root "artifacts\installer"
$installerPath = Join-Path $installerDirectory "TouchRpc-Setup-$Runtime.msi"
$wixSource = Join-Path $root "installer\TouchRpc.wxs"

& $publishScript -Runtime $Runtime -Configuration $Configuration
if ($LASTEXITCODE -ne 0) { throw "Publish-Release.ps1 failed with exit code $LASTEXITCODE." }

$wix = Get-Command wix -ErrorAction SilentlyContinue
if ($null -eq $wix) {
    throw "WiX Toolset v5 was not found. Install it with: dotnet tool install --global wix"
}

Remove-Item $installerDirectory -Recurse -Force -ErrorAction SilentlyContinue
New-Item $installerDirectory -ItemType Directory -Force | Out-Null

& $wix.Source build $wixSource `
    -arch x64 `
    -d "PublishDir=$publishDirectory" `
    -intermediatefolder (Join-Path $installerDirectory "obj") `
    -pdbtype none `
    -out $installerPath
if ($LASTEXITCODE -ne 0) { throw "WiX build failed with exit code $LASTEXITCODE." }

Write-Output "Installer: $installerPath"