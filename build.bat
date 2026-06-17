@echo off
@echo off
REM ============================================
REM  MazizTool - Build Script
REM  Ultimate Windows Recovery & Anti-Malware Hub
REM  (Also buildable via: dotnet publish -r win-x64 on macOS/Linux)
REM ============================================
echo.

REM Check for .NET 8 SDK
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] .NET 8 SDK is not installed!
    echo Download from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo [INFO] .NET SDK found: 
dotnet --version
echo.

echo [BUILD] Restoring packages...
dotnet restore MazizTool\MazizTool.csproj
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Failed to restore packages
    pause
    exit /b 1
)

echo.
echo [BUILD] Publishing single-file executable...
dotnet publish MazizTool\MazizTool.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:PublishTrimmed=true ^
    -p:TrimMode=partial ^
    -o publish

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Build failed
    pause
    exit /b 1
)

echo.
echo ============================================
echo  BUILD SUCCESSFUL!
echo  Output: publish\MazizTool.exe
echo ============================================
echo.
echo [INFO] The EXE is fully self-contained and requires NO .NET runtime.
echo [INFO] Can be run from USB, Windows PE, Safe Mode, or Recovery Environment.
echo.
pause
