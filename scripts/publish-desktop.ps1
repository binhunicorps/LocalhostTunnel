param(
    [string]$Version = "",
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
$releaseDir = Join-Path $repoRoot "artifacts\release"
$desktopExe = Join-Path $publishDir "LocalhostTunnel.Desktop.exe"
$updaterExe = Join-Path $publishDir "LocalhostTunnel.Updater.exe"
$issFile = Join-Path $repoRoot "packaging\LocalhostTunnel.iss"

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = $env:APP_VERSION
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = "1.0.15"
}

$numericVersion = ($Version -split "-", 2)[0]
$versionParts = $numericVersion.Split(".")
if ($versionParts.Count -lt 2 -or $versionParts.Count -gt 4) {
    throw "Version format must be numeric with 2-4 dot-separated parts. Received: $Version"
}

while ($versionParts.Count -lt 4) {
    $versionParts += "0"
}

$assemblyVersion = $versionParts -join "."
$portableAssetName = "LocalhostTunnel-Portable-win-x64-v$Version.zip"
$installerAssetName = "LocalhostTunnel-Setup-win-x64-v$Version.exe"
$portableAssetPath = Join-Path $releaseDir $portableAssetName
$installerAssetPath = Join-Path $releaseDir $installerAssetName

if (-not $SkipBuild) {
    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

    dotnet publish $desktopProject `
        -c Release `
        -r win-x64 `
        -p:PublishSingleFile=true `
        -p:SelfContained=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:Version=$Version `
        -p:AssemblyVersion=$assemblyVersion `
        -p:FileVersion=$assemblyVersion `
        -p:InformationalVersion=$Version `
        -o $publishDir

    dotnet publish $updaterProject `
        -c Release `
        -r win-x64 `
        -p:PublishSingleFile=true `
        -p:SelfContained=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:Version=$Version `
        -p:AssemblyVersion=$assemblyVersion `
        -p:FileVersion=$assemblyVersion `
        -p:InformationalVersion=$Version `
        -o $publishDir
}

if (-not (Test-Path $desktopExe)) {
    throw "Desktop publish output missing: $desktopExe"
}

if (-not (Test-Path $updaterExe)) {
    throw "Updater publish output missing: $updaterExe"
}

New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
if (Test-Path $portableAssetPath) {
    Remove-Item -LiteralPath $portableAssetPath -Force
}
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $portableAssetPath -CompressionLevel Optimal

if (-not $SkipInstaller) {
    if (-not (Get-Command iscc -ErrorAction SilentlyContinue)) {
        Write-Warning "Inno Setup Compiler (iscc) not found. Skipping installer build."
    }
    else {
        New-Item -ItemType Directory -Path $installerDir -Force | Out-Null
        $isccOutputBaseName = "LocalhostTunnel-Setup-win-x64-v$Version"
        & iscc "/DAppVersion=$Version" "/DOutputBaseFilename=$isccOutputBaseName" $issFile

        $installerOutput = Join-Path $installerDir "$isccOutputBaseName.exe"
        if (Test-Path $installerOutput) {
            Copy-Item -LiteralPath $installerOutput -Destination $installerAssetPath -Force
        }
    }
}

$rootLauncher = Join-Path $repoRoot "LocalhostTunnel.Desktop.exe"
try {
    Copy-Item -LiteralPath $desktopExe -Destination $rootLauncher -Force
}
catch {
    Write-Warning "Unable to update root launcher ($rootLauncher). The file may be in use."
}

Write-Host "Desktop publish completed."
Write-Host "Output: $publishDir"
Write-Host "Portable: $portableAssetPath"
if (Test-Path $installerAssetPath) {
    Write-Host "Installer: $installerAssetPath"
}
