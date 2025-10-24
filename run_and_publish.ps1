# This PowerShell script is used to run and publish the application.

# Navigate to the application directory
Set-Location -Path "$PSScriptRoot"

# Run the application
#dotnet run --project "src\WinWork.UI\WinWork.UI.csproj"

# Publish the application as a self-contained portable app
# Replace <runtime-identifier> with the appropriate RID for your target platform
# Example: win-x64 for Windows 64-bit
# Clean any previous build artifacts to ensure fresh build
Write-Host "Cleaning previous build artifacts..."
dotnet clean "WinWork.sln" -c Release

# Restore packages
Write-Host "Restoring packages..."
dotnet restore "WinWork.sln"

# First ensure Models project builds correctly as a library
Write-Host "Building Models project as library..."
dotnet build "src\WinWork.Models\WinWork.Models.csproj" -c Release -p:OutputType=Library

# Build the UI project first to avoid CS5001 issues
Write-Host "Building UI project..."
dotnet build "src\WinWork.UI\WinWork.UI.csproj" -c Release

# Publish as single-file self-contained portable executable (~140-180MB with all .NET runtime bundled)
Write-Host "Publishing application as single-file self-contained portable executable..."
dotnet publish "src\WinWork.UI\WinWork.UI.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -p:IncludeNativeLibrariesForSelfExtract=true -o "C:\bin\WinWork"

# Remove debug files for production deployment
Write-Host "Cleaning up debug files..."
Remove-Item "C:\bin\WinWork\*.pdb" -Force -ErrorAction SilentlyContinue

Write-Host "Application has been published as a single-file self-contained portable executable to C:\bin\WinWork"
Write-Host "Main executable: WinWork.UI.exe (~180MB with full .NET 9 runtime included)"