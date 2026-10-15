param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    $localDotnet = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet\dotnet.exe"
    if (-not (Test-Path $localDotnet)) {
        throw "The .NET SDK was not found on PATH or in the user-local installation."
    }
    $dotnetPath = $localDotnet
}
else {
    $dotnetPath = $dotnet.Source
}

$artifactRoot = Join-Path $root "artifacts"
$publishDirectory = Join-Path $artifactRoot "TouchRpc-$Runtime"
$archivePath = Join-Path $artifactRoot "TouchRpc-$Runtime.zip"

Remove-Item $publishDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $archivePath -Force -ErrorAction SilentlyContinue
New-Item $publishDirectory -ItemType Directory -Force | Out-Null

& $dotnetPath publish (Join-Path $root "src\TouchRpc.App\TouchRpc.App.csproj") `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDirectory `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

Copy-Item (Join-Path $root "README.md") $publishDirectory
Copy-Item (Join-Path $root "THIRD-PARTY-NOTICES.md") $publishDirectory
Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $archivePath

Write-Output "Published folder: $publishDirectory"
Write-Output "Portable archive: $archivePath"