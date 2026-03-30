param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version muss das Format Major.Minor.Patch haben. Aktuell: $Version"
}

$exportPresetsPath = Join-Path $PSScriptRoot '..\..\export_presets.cfg'
if (-not (Test-Path $exportPresetsPath)) {
    throw "export_presets.cfg wurde nicht gefunden: $exportPresetsPath"
}

$windowsVersion = "$Version.0"
$content = Get-Content $exportPresetsPath -Raw
$content = [regex]::Replace($content, 'application/file_version="[^"]*"', ('application/file_version="' + $windowsVersion + '"'))
$content = [regex]::Replace($content, 'application/product_version="[^"]*"', ('application/product_version="' + $windowsVersion + '"'))
Set-Content -Path $exportPresetsPath -Value $content -NoNewline

Write-Host "Windows-Dateiversion synchronisiert: $windowsVersion"
