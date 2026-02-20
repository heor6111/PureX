# PureX Build Script - Portable Version
param(
    [switch]$Clean,
    [string]$Version,
    [string]$OutputDir = ".\publish"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $ScriptDir "src\PureX"
$ToolsDir = Join-Path $ScriptDir "tools\ffmpeg\bin"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  PureX Build Script" -ForegroundColor Cyan
Write-Host "  Portable Version" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$currentVersion = "1.0.0"
if ($Version) {
    $currentVersion = $Version
}
$buildNumber = Get-Date -Format "yyyyMMdd"
$fullVersion = "$currentVersion.$buildNumber"
Write-Host "Version: $fullVersion" -ForegroundColor White

Write-Host "[1/6] Cleaning..." -ForegroundColor Yellow
$outputPath = Join-Path $ScriptDir $OutputDir
if (Test-Path $outputPath) {
    Remove-Item -Path $outputPath -Recurse -Force
}
if ($Clean) {
    $dirs = @("src\PureX\bin", "src\PureX\obj", "src\PureX.Core\bin", "src\PureX.Core\obj")
    foreach ($d in $dirs) {
        $p = Join-Path $ScriptDir $d
        if (Test-Path $p) { Remove-Item -Path $p -Recurse -Force }
    }
}
Write-Host "      Done" -ForegroundColor Green

Write-Host "[2/6] Checking FFmpeg..." -ForegroundColor Yellow
$ffmpegExe = Join-Path $ToolsDir "ffmpeg.exe"
$ffprobeExe = Join-Path $ToolsDir "ffprobe.exe"
if (-not (Test-Path $ffmpegExe)) { Write-Host "ERROR: ffmpeg.exe not found" -ForegroundColor Red; exit 1 }
if (-not (Test-Path $ffprobeExe)) { Write-Host "ERROR: ffprobe.exe not found" -ForegroundColor Red; exit 1 }
Write-Host "      FFmpeg found" -ForegroundColor Green

Write-Host "[3/6] Building..." -ForegroundColor Yellow
$result = dotnet build $ProjectDir -c Release --nologo -v q 2>&1
if ($LASTEXITCODE -ne 0) { 
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    Write-Host $result
    exit 1 
}
Write-Host "      Build successful" -ForegroundColor Green

Write-Host "[4/6] Publishing..." -ForegroundColor Yellow
dotnet publish $ProjectDir -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -p:Version="$fullVersion" `
    -p:FileVersion="$fullVersion" `
    -p:AssemblyVersion="$fullVersion" `
    -o $outputPath --nologo -v q
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Publish failed" -ForegroundColor Red; exit 1 }
Write-Host "      Publish successful" -ForegroundColor Green

Write-Host "[5/6] Copying FFmpeg..." -ForegroundColor Yellow
$ffmpegDestDir = Join-Path $outputPath "ffmpeg"
New-Item -ItemType Directory -Force -Path $ffmpegDestDir | Out-Null
Copy-Item $ffmpegExe $ffmpegDestDir -Force
Copy-Item $ffprobeExe $ffmpegDestDir -Force
Write-Host "      FFmpeg copied" -ForegroundColor Green

Write-Host "[6/6] Generating checksum..." -ForegroundColor Yellow
$exeFile = Join-Path $outputPath "PureX.exe"
if (-not (Test-Path $exeFile)) { Write-Host "ERROR: Output file not found" -ForegroundColor Red; exit 1 }

$fileInfo = Get-Item $exeFile
$fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
$sha256 = (Get-FileHash -Path $exeFile -Algorithm SHA256).Hash

$totalSize = (Get-ChildItem $outputPath -Recurse -File | Measure-Object -Property Length -Sum).Sum
$totalSizeMB = [math]::Round($totalSize / 1MB, 2)

$checksumFile = Join-Path $outputPath "checksum.txt"
@"
PureX v$currentVersion
==============================
File: PureX.exe
Version: $fullVersion
EXE Size: $fileSizeMB MB
Total Size: $totalSizeMB MB
SHA256: $sha256
Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

This is a portable application.
No installation required.
Just extract and run PureX.exe

Contents:
- PureX.exe (Main Application)
- ffmpeg/ (FFmpeg Tools)
  - ffmpeg.exe
  - ffprobe.exe
"@ | Out-File -FilePath $checksumFile -Encoding UTF8

Write-Host "      EXE Size: $fileSizeMB MB" -ForegroundColor Green
Write-Host "      Total Size: $totalSizeMB MB" -ForegroundColor Green
Write-Host "      SHA256: $sha256" -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "  Output: $outputPath" -ForegroundColor White
Write-Host "  Total Size: $totalSizeMB MB" -ForegroundColor White
Write-Host "  Type: Portable Application" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

explorer $outputPath
