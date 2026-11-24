# Yerel olarak dağıtıma hazır tek-dosya (single-file) EXE ve ZIP paketlerini oluşturur.
param(
    [string]$Version = '1.0.0',
    [switch]$SkipStandalone
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\AcerKontrol\AcerKontrol.csproj'
$outRoot = Join-Path $root 'publish\release'
$tag = "v$Version"

if (Test-Path $outRoot) { Remove-Item $outRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $outRoot | Out-Null

Write-Host "Birim testleri çalıştırılıyor..."
dotnet test (Join-Path $root 'AcerKontrol.sln') -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw "Testler başarısız oldu." }

# 1. Portable (küçük boyutlu, sistemde .NET 9 Desktop Runtime gerektirir)
$portableDir = Join-Path $outRoot 'stage-portable'
Write-Host "Portable tek-dosya EXE derleniyor..."
dotnet publish $project -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true -p:DebugType=none -p:DebugSymbols=false `
    -p:Version=$Version -o $portableDir --nologo --disable-build-servers
if ($LASTEXITCODE -ne 0) { throw "Portable derleme başarısız." }
Copy-Item (Join-Path $root 'LICENSE'), (Join-Path $root 'README.md') $portableDir
Compress-Archive -Path "$portableDir\*" -DestinationPath (Join-Path $outRoot "AcerKontrol-$tag-win-x64-portable.zip") -Force
Remove-Item $portableDir -Recurse -Force

# 2. Standalone (.NET 9 dahili tek-dosya EXE)
if (-not $SkipStandalone) {
    $standaloneDir = Join-Path $outRoot 'stage-standalone'
    Write-Host "Standalone (.NET dahili) tek-dosya EXE derleniyor..."
    dotnet publish $project -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false `
        -p:Version=$Version -o $standaloneDir --nologo --disable-build-servers
    if ($LASTEXITCODE -ne 0) { throw "Standalone derleme başarısız." }
    Copy-Item (Join-Path $root 'LICENSE'), (Join-Path $root 'README.md') $standaloneDir
    Copy-Item (Join-Path $standaloneDir 'AcerKontrol.exe') (Join-Path $outRoot "AcerKontrol-$tag-win-x64.exe")
    Compress-Archive -Path "$standaloneDir\*" -DestinationPath (Join-Path $outRoot "AcerKontrol-$tag-win-x64-standalone.zip") -Force
    Remove-Item $standaloneDir -Recurse -Force
}

# SHA256 doğrulama dosyası
Get-ChildItem $outRoot -File | Sort-Object Name | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($_.Name)"
} | Set-Content -Encoding utf8NoBOM (Join-Path $outRoot 'SHA256SUMS.txt')

Write-Host "`nRelease paketleri hazır: $outRoot"
Get-ChildItem $outRoot -File | Select-Object Name, @{N='SizeMB';E={[math]::Round($_.Length / 1MB, 2)}} | Format-Table -AutoSize
