[CmdletBinding(DefaultParameterSetName = "Local")]
param(
    [Parameter(ParameterSetName = "Local")]
    [string]$InstallerPath = (Join-Path $PSScriptRoot "..\artifacts\installer\TouchRpc-Setup-win-x64.msi"),

    [Parameter(Mandatory, ParameterSetName = "Download")]
    [uri]$InstallerUri,

    [Parameter(Mandatory, ParameterSetName = "Download")]
    [ValidatePattern("^[A-Fa-f0-9]{64}$")]
    [string]$Sha256
)

$ErrorActionPreference = "Stop"
$temporaryDirectory = $null

try {
    if ($PSCmdlet.ParameterSetName -eq "Download") {
        if ($InstallerUri.Scheme -ne "https") {
            throw "InstallerUri must use HTTPS."
        }

        $temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("TouchRpc-" + [guid]::NewGuid())
        New-Item $temporaryDirectory -ItemType Directory | Out-Null
        $resolvedInstallerPath = Join-Path $temporaryDirectory "TouchRpc-Setup-win-x64.msi"
        Invoke-WebRequest -Uri $InstallerUri -OutFile $resolvedInstallerPath -UseBasicParsing
    }
    else {
        $resolvedInstallerPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($InstallerPath)
    }

    if (-not (Test-Path $resolvedInstallerPath -PathType Leaf)) {
        throw "Installer was not found: $resolvedInstallerPath"
    }

    $actualHash = (Get-FileHash $resolvedInstallerPath -Algorithm SHA256).Hash
    if ($PSCmdlet.ParameterSetName -eq "Download" -and $actualHash -ne $Sha256) {
        throw "SHA-256 verification failed. Expected $Sha256 but received $actualHash."
    }

    Write-Output "SHA-256: $actualHash"
    $process = Start-Process msiexec.exe -ArgumentList @(
        "/i",
        ('"{0}"' -f $resolvedInstallerPath),
        "/passive",
        "/norestart"
    ) -Wait -PassThru

    if ($process.ExitCode -notin @(0, 1641, 3010)) {
        throw "MSI installation failed with exit code $($process.ExitCode)."
    }

    Write-Output "TOUCH RPC installation completed successfully."
    if ($process.ExitCode -in @(1641, 3010)) {
        Write-Warning "Windows reported that a restart is required."
    }
}
finally {
    if ($null -ne $temporaryDirectory) {
        Remove-Item $temporaryDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }
}