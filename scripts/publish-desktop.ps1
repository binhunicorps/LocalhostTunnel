param(
    [switch]$SkipBuild,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")

$desktopProject = Join-Path $repoRoot "src\LocalhostTunnel.Desktop\LocalhostTunnel.Desktop.csproj"
$updaterProject = Join-Path $repoRoot "src\LocalhostTunnel.Updater\LocalhostTunnel.Updater.csproj"
$publishDir = Join-Path $repoRoot "artifacts\publish"
$installerDir = Join-Path $repoRoot "artifacts\installer"
$desktopExe = Join-Path $publishDir "LocalhostTunnel.Desktop.exe"
$updaterExe = Join-Path $publishDir "LocalhostTunnel.Updater.exe"
$issFile = Join-Path $repoRoot "packaging\LocalhostTunnel.iss"

if (-not $SkipBuild) {
    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

    dotnet publish $desktopProject `
        -c Release `
        -r win-x64 `
        -p:PublishSingleFile=true `
        -p:SelfContained=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $publishDir

    dotnet publish $updaterProject `
        -c Release `
        -r win-x64 `
        -p:PublishSingleFile=true `
        -p:SelfContained=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $publishDir
}

if (-not (Test-Path $desktopExe)) {
    throw "Desktop publish output missing: $desktopExe"
}

if (-not (Test-Path $updaterExe)) {
    throw "Updater publish output missing: $updaterExe"
}

if (-not $SkipInstaller) {
    if (-not (Get-Command iscc -ErrorAction SilentlyContinue)) {
        Write-Warning "Inno Setup Compiler (iscc) not found. Skipping installer build."
    }
    else {
        New-Item -ItemType Directory -Path $installerDir -Force | Out-Null
        iscc $issFile
    }
}

$rootLauncher = Join-Path $repoRoot "LocalhostTunnel.Desktop.exe"
Copy-Item -LiteralPath $desktopExe -Destination $rootLauncher -Force

Write-Host "Desktop publish completed."
Write-Host "Output: $publishDir"
