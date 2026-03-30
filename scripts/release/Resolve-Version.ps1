param(
    [string]$VersionOverride = "",
    [ValidateSet("none","patch","minor","major")]
    [string]$Bump = "patch",
    [string]$BuildNumber = "0"
)

$versionFile = Join-Path $PSScriptRoot "..\..\VERSION"
$buildFile   = Join-Path $PSScriptRoot "..\..\BUILD"

function Parse-Version([string]$value) {
    $clean = ($value ?? "").Trim()
    if (-not $clean) { throw "VERSION ist leer." }

    $parts = $clean.Split('.')
    if ($parts.Count -ne 3) { throw "VERSION muss das Format Major.Minor.Patch haben. Aktuell: $clean" }

    return [pscustomobject]@{
        Major = [int]$parts[0]
        Minor = [int]$parts[1]
        Patch = [int]$parts[2]
    }
}

function To-VersionString($v) {
    return "$($v.Major).$($v.Minor).$($v.Patch)"
}

$currentRaw = (Get-Content $versionFile -Raw).Trim()
$current = Parse-Version $currentRaw

if ($VersionOverride.Trim()) {
    $next = Parse-Version $VersionOverride
}
else {
    $next = [pscustomobject]@{
        Major = $current.Major
        Minor = $current.Minor
        Patch = $current.Patch
    }

    switch ($Bump) {
        "major" {
            $next.Major++
            $next.Minor = 0
            $next.Patch = 0
        }
        "minor" {
            $next.Minor++
            $next.Patch = 0
        }
        "patch" {
            $next.Patch++
        }
        "none" {
        }
    }
}

$nextString = To-VersionString $next

Set-Content -Path $versionFile -Value $nextString -NoNewline
Set-Content -Path $buildFile -Value $BuildNumber -NoNewline

Write-Host "VERSION -> $nextString"
Write-Host "BUILD   -> $BuildNumber"
