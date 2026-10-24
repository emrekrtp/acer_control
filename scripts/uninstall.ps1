# AcerKontrol'ü durdurur, zamanlanmış görevini ve kurulum klasörünü kaldırır.
# Ayar dosyası (%APPDATA%\AcerKontrol\settings.json) yerinde bırakılır.
$ErrorActionPreference = 'Stop'

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Start-Process pwsh -Verb RunAs -Wait -ArgumentList '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`""
    exit
}

$TaskName = 'AcerKontrol'
# Program Files: yönetici olmayan süreçler yükseltilmiş çalışan exe'yi değiştiremesin.
$InstallDir = Join-Path $env:ProgramFiles 'AcerKontrol'
$LegacyDir = Join-Path $env:LOCALAPPDATA 'Programs\AcerKontrol'

Get-Process AcerKontrol -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
}
foreach ($dir in $InstallDir, $LegacyDir) {
    if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
}

Write-Host 'Acer Kontrol kaldırıldı. Pil sınırı son bırakıldığı durumda kalır.'
Start-Sleep -Seconds 3
