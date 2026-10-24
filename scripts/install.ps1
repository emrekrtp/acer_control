# AcerKontrol'ü derler, %LOCALAPPDATA%\Programs\AcerKontrol altına kurar ve
# oturum açılışında yönetici yetkisiyle (UAC sormadan) başlatan zamanlanmış görevi kaydeder.
$ErrorActionPreference = 'Stop'

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Start-Process pwsh -Verb RunAs -Wait -ArgumentList '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`""
    exit
}

$TaskName = 'AcerKontrol'
$InstallDir = Join-Path $env:LOCALAPPDATA 'Programs\AcerKontrol'
$Project = Join-Path $PSScriptRoot '..\src\AcerKontrol\AcerKontrol.csproj'
$Exe = Join-Path $InstallDir 'AcerKontrol.exe'

Write-Host 'Çalışan AcerKontrol durduruluyor...'
Get-Process AcerKontrol -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

Write-Host "Derleniyor ve $InstallDir altına kuruluyor..."
dotnet publish $Project -c Release -r win-x64 --self-contained false -o $InstallDir --nologo --disable-build-servers
if ($LASTEXITCODE -ne 0) { throw "dotnet publish başarısız (kod $LASTEXITCODE)" }

Write-Host "Zamanlanmış görev '$TaskName' kaydediliyor..."
$user = "$env:USERDOMAIN\$env:USERNAME"
$action = New-ScheduledTaskAction -Execute $Exe -WorkingDirectory $InstallDir
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $user
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
    -ExecutionTimeLimit ([TimeSpan]::Zero) -MultipleInstances IgnoreNew
Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal `
    -Settings $settings -Force | Out-Null

Start-ScheduledTask -TaskName $TaskName
Write-Host 'Kurulum tamam. Acer Kontrol tepside çalışıyor.'
Start-Sleep -Seconds 3
