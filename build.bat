@echo off
chcp 65001 >nul
echo.
echo ========================================
echo   PureX Build Script
echo ========================================
echo.

:: Check FFmpeg
if not exist "tools\ffmpeg\bin\ffmpeg.exe" (
    echo [ERROR] FFmpeg not found!
    echo Please download FFmpeg and extract to tools\ffmpeg\
    echo Download: https://ffmpeg.org/download.html
    pause
    exit /b 1
)

:: Clean
echo [1/4] Cleaning...
if exist "publish" rmdir /s /q "publish"

:: Build
echo [2/4] Building...
dotnet build src\PureX\PureX.csproj -c Release --nologo -v q
if errorlevel 1 (
    echo [ERROR] Build failed!
    pause
    exit /b 1
)

:: Publish
echo [3/4] Publishing...
dotnet publish src\PureX\PureX.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false -o publish --nologo -v q
if errorlevel 1 (
    echo [ERROR] Publish failed!
    pause
    exit /b 1
)

:: Copy FFmpeg
echo [4/4] Copying FFmpeg...
mkdir "publish\ffmpeg" 2>nul
copy /y "tools\ffmpeg\bin\ffmpeg.exe" "publish\ffmpeg\" >nul
copy /y "tools\ffmpeg\bin\ffprobe.exe" "publish\ffmpeg\" >nul

echo.
echo ========================================
echo   Build Complete!
echo   Output: publish\
echo ========================================
echo.

explorer publish
pause
