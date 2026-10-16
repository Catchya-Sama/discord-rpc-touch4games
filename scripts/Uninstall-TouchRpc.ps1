[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$uninstallPaths = @(
    "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*",
    "HKCU:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*",
    "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*",
    "HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*"
)

$registrations = @(
    Get-ItemProperty $uninstallPaths -ErrorAction SilentlyContinue |
        Where-Object {
            $_.DisplayName -eq "TOUCH RPC" -and
            $_.Publisher -eq "TOUCH RPC" -and
            $_.WindowsInstaller -eq 1
        } |
        Sort-Object PSChildName -Unique
)

if ($registrations.Count -eq 0) {
    Write-Output "TOUCH RPC is not installed for the current Windows user."
    exit 0
}

if ($registrations.Count -gt 1) {
    throw "Multiple TOUCH RPC MSI registrations were found for the current Windows user."
}

$registration = $registrations[0]
$productCode = $registration.PSChildName
if ($productCode -notmatch '^\{[A-Fa-f0-9]{8}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{12}\}$') {
    throw "The TOUCH RPC MSI registration has an invalid ProductCode: $productCode"
}

Write-Output "Uninstalling TOUCH RPC $($registration.DisplayVersion)..."
$process = Start-Process msiexec.exe -ArgumentList @(
    "/x",
    $productCode,
    "/passive",
    "/norestart"
) -Wait -PassThru

if ($process.ExitCode -notin @(0, 1605, 1641, 3010)) {
    throw "MSI uninstall failed with exit code $($process.ExitCode)."
}

Write-Output "TOUCH RPC uninstall completed successfully."
if ($process.ExitCode -in @(1641, 3010)) {
    Write-Warning "Windows reported that a restart is required."
}