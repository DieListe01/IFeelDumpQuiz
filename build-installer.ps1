$ErrorActionPreference = 'Stop'

$issPath = Join-Path $PSScriptRoot 'IFeelDumpQuiz.iss'
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
    throw "VERSION ist leer."
}

$build = ''
if (Test-Path $buildFile) {
    $build = (Get-Content $buildFile -Raw).Trim()
}

$iscc = @(
    "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    throw "ISCC.exe wurde nicht gefunden. Bitte Inno Setup 6 installieren."
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

& $iscc @arguments
if ($LASTEXITCODE -ne 0) {
    throw "ISCC meldete Fehlercode $LASTEXITCODE."
}
