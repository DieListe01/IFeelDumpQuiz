$ErrorActionPreference = 'Stop'

$issPath = Join-Path $PSScriptRoot 'installer\IFeelDumpQuiz.iss'
$versionFile = Join-Path $PSScriptRoot 'VERSION'
$buildFile = Join-Path $PSScriptRoot 'BUILD'

if (-not (Test-Path $issPath)) {
    throw "Installer-Skript nicht gefunden: $issPath"
}

if (-not (Test-Path $versionFile)) {
    throw "VERSION-Datei nicht gefunden: $versionFile"
}

$version = (Get-Content $versionFile -Raw).Trim()
if (-not $version) {
    throw 'VERSION ist leer.'
}

$build = ''
if (Test-Path $buildFile) {
    $build = (Get-Content $buildFile -Raw).Trim()
}

$pathCandidates = @()

if (${env:ProgramFiles(x86)}) {
    $pathCandidates += (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe')
}
if ($env:ProgramFiles) {
    $pathCandidates += (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
}
if ($env:ChocolateyInstall) {
    $pathCandidates += (Join-Path $env:ChocolateyInstall 'bin\ISCC.exe')
}

$cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if ($cmd -and $cmd.Source) {
    $pathCandidates = @($cmd.Source) + $pathCandidates
}

$iscc = $pathCandidates |
    Where-Object { $_ -and (Test-Path $_) } |
    Select-Object -Unique -First 1

if (-not $iscc) {
    Write-Host 'Gepruefte ISCC-Pfade:'
    $pathCandidates | Where-Object { $_ } | ForEach-Object { Write-Host " - $_" }
    throw 'ISCC.exe wurde nicht gefunden. Bitte Inno Setup 6 installieren oder den Pfad pruefen.'
}

$arguments = @(
    "/DMyAppVersion=$version"
)

if ($build) {
    $arguments += "/DMyBuildNumber=$build"
}

$arguments += $issPath

Write-Host "Baue Installer mit Version $version"
if ($build) {
    Write-Host "Buildnummer: $build"
}
Write-Host "Verwendetes ISCC: $iscc"

& $iscc @arguments
if ($LASTEXITCODE -ne 0) {
    throw "ISCC meldete Fehlercode $LASTEXITCODE."
}
