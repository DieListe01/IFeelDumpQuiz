#requires -Version 5.1
[CmdletBinding()]
param(
    [ValidateSet('workflow','local')]
    [string]$Mode = 'workflow',

    [string]$Version = '',

    [ValidateSet('none','patch','minor','major')]
    [string]$Bump = 'patch',

    [string]$BuildNumber = '',

    [string]$Branch = 'main',
    [string]$WorkflowName = 'Windows Release',
    [string]$Repo = '',

    [switch]$Watch,
    [switch]$PreRelease,
    [switch]$Draft,
    [switch]$AllowDirty,

    [switch]$Commit,
    [switch]$Tag,
    [switch]$Push,
    [switch]$BuildLocal,
    [switch]$SkipInstaller,
    [switch]$CreateGitHubRelease,

    [string]$GodotExe = '',
    [string]$Solution = 'IFeelDumpQuiz.sln',
    [string]$ExportPreset = 'Windows Desktop'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$VersionFile = Join-Path $ScriptRoot 'VERSION'
$BuildFile = Join-Path $ScriptRoot 'BUILD'
$ExportPresetsFile = Join-Path $ScriptRoot 'export_presets.cfg'
$BuildInstallerScript = Join-Path $ScriptRoot 'build-installer.ps1'
$DistDir = Join-Path $ScriptRoot 'dist'
$WindowsDistDir = Join-Path $DistDir 'windows'
$InstallerDistDir = Join-Path $DistDir 'installer'
$PortableZip = Join-Path $DistDir 'IFeelDumpQuiz-win64.zip'

function Write-Step([string]$Text) {
    Write-Host "`n=== $Text ===" -ForegroundColor Cyan
}

function Write-Info([string]$Text) {
    Write-Host $Text -ForegroundColor Gray
}

function Write-Success([string]$Text) {
    Write-Host $Text -ForegroundColor Green
}

function Write-Warn([string]$Text) {
    Write-Warning $Text
}

function Set-TextFileNoNewline([string]$Path, [string]$Value) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Value, $utf8NoBom)
}

function Require-File([string]$Path, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Label nicht gefunden: $Path"
    }
}

function Require-Command([string]$Name) {
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if (-not $cmd) {
        throw "Befehl '$Name' wurde nicht gefunden. Bitte installieren oder in PATH aufnehmen."
    }
}

function Invoke-External {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,

        [switch]$IgnoreExitCode
    )

    & $Command @Arguments
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) {
        $joined = ($Arguments -join ' ')
        throw "Befehl fehlgeschlagen: $Command $joined (ExitCode $LASTEXITCODE)"
    }
}

function Parse-Version([string]$Value) {
    $clean = ($Value | ForEach-Object { $_ })
    if ($null -eq $clean) { $clean = '' }
    $clean = $clean.Trim()

    if ($clean -notmatch '^(\d+)\.(\d+)\.(\d+)$') {
        throw "Version muss das Format Major.Minor.Patch haben. Aktuell: '$clean'"
    }

    [pscustomobject]@{
        Major = [int]$Matches[1]
        Minor = [int]$Matches[2]
        Patch = [int]$Matches[3]
    }
}

function Format-Version($VersionObject) {
    return ('{0}.{1}.{2}' -f $VersionObject.Major, $VersionObject.Minor, $VersionObject.Patch)
}

function Get-NextVersion([string]$CurrentVersion, [string]$VersionOverride, [string]$BumpMode) {
    if ($VersionOverride -and $VersionOverride.Trim()) {
        return (Format-Version (Parse-Version $VersionOverride))
    }

    $current = Parse-Version $CurrentVersion
    $next = [pscustomobject]@{
        Major = $current.Major
        Minor = $current.Minor
        Patch = $current.Patch
    }

    switch ($BumpMode) {
        'major' {
            $next.Major++
            $next.Minor = 0
            $next.Patch = 0
        }
        'minor' {
            $next.Minor++
            $next.Patch = 0
        }
        'patch' {
            $next.Patch++
        }
        'none' {
        }
        default {
            throw "Unbekannter Bump-Modus: $BumpMode"
        }
    }

    return (Format-Version $next)
}

function Get-CurrentVersion() {
    Require-File -Path $VersionFile -Label 'VERSION-Datei'
    return (Get-Content -LiteralPath $VersionFile -Raw).Trim()
}

function Get-DefaultBuildNumber() {
    try {
        $count = (& git rev-list --count HEAD 2>$null)
        if ($LASTEXITCODE -eq 0 -and $count) {
            return ($count | Out-String).Trim()
        }
    }
    catch {
    }

    return '0'
}

function Get-BuildNumber([string]$RequestedBuildNumber) {
    if ($RequestedBuildNumber -and $RequestedBuildNumber.Trim()) {
        return $RequestedBuildNumber.Trim()
    }

    return (Get-DefaultBuildNumber)
}

function Sync-ExportPresetVersions([string]$Path, [string]$VersionString, [string]$BuildString) {
    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Warn "export_presets.cfg wurde nicht gefunden und konnte nicht aktualisiert werden."
        return
    }

    $numericBuild = 0
    if ($BuildString -match '^\d+$') {
        $numericBuild = [int]$BuildString
    }

    $windowsVersion = "$VersionString.$numericBuild"
    $content = Get-Content -LiteralPath $Path -Raw
    $updated = $content
    $updated = [regex]::Replace($updated, '(?m)^application/file_version=.*$', "application/file_version=`"$windowsVersion`"")
    $updated = [regex]::Replace($updated, '(?m)^application/product_version=.*$', "application/product_version=`"$windowsVersion`"")

    if ($updated -ne $content) {
        Set-TextFileNoNewline -Path $Path -Value $updated
        Write-Info "export_presets.cfg aktualisiert -> $windowsVersion"
    }
    else {
        Write-Warn "application/file_version bzw. application/product_version wurde in export_presets.cfg nicht gefunden."
    }
}

function Assert-GitClean() {
    if ($AllowDirty) {
        return
    }

    $status = (& git status --porcelain 2>$null | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw 'Git-Status konnte nicht ermittelt werden.'
    }

    if ($status) {
        throw "Arbeitsverzeichnis ist nicht sauber. Bitte committen/stashen oder -AllowDirty verwenden.`n$status"
    }
}

function Invoke-Git([string[]]$Arguments) {
    Invoke-External -Command 'git' -Arguments $Arguments
}

function Ensure-GitHubCliAuthenticated() {
    Require-Command 'gh'

    & gh auth status *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "GitHub CLI ist nicht angemeldet. Bitte zuerst 'gh auth login' ausführen."
    }
}

function Invoke-WorkflowMode {
    $currentVersion = Get-CurrentVersion
    $predictedVersion = Get-NextVersion -CurrentVersion $currentVersion -VersionOverride $Version -BumpMode $Bump

    Write-Step "GitHub-Workflow starten"
    Write-Info "Aktuelle VERSION : $currentVersion"
    Write-Info "Geplante VERSION: $predictedVersion"
    Write-Info "Branch          : $Branch"
    Write-Info "Workflow        : $WorkflowName"

    Ensure-GitHubCliAuthenticated

    $args = @('workflow', 'run', $WorkflowName, '--ref', $Branch)
    if ($Repo) {
        $args += @('--repo', $Repo)
    }
    $args += @('-f', "version=$Version")
    $args += @('-f', "bump=$Bump")
    $args += @('-f', ('prerelease=' + ($PreRelease.IsPresent.ToString().ToLowerInvariant())))
    $args += @('-f', ('draft=' + ($Draft.IsPresent.ToString().ToLowerInvariant())))

    Invoke-External -Command 'gh' -Arguments $args
    Write-Success 'Workflow wurde an GitHub uebergeben.'

    if (-not $Watch) {
        Write-Info 'Hinweis: Mit -Watch kannst du direkt auf den Run warten.'
        return
    }

    Write-Step 'Neuesten Workflow-Run suchen'
    Start-Sleep -Seconds 5

    $runId = $null
    $runUrl = ''
    for ($i = 0; $i -lt 18; $i++) {
        $listArgs = @('run', 'list', '--workflow', $WorkflowName, '--branch', $Branch, '--limit', '1', '--json', 'databaseId,url,displayTitle,status,conclusion')
        if ($Repo) {
            $listArgs += @('--repo', $Repo)
        }

        $json = (& gh @listArgs | Out-String)
        if ($LASTEXITCODE -eq 0 -and $json.Trim()) {
            $runs = $json | ConvertFrom-Json
            if ($runs -and $runs[0].databaseId) {
                $runId = [string]$runs[0].databaseId
                $runUrl = [string]$runs[0].url
                break
            }
        }

        Start-Sleep -Seconds 5
    }

    if (-not $runId) {
        Write-Warn 'Workflow-Run wurde nicht automatisch gefunden. Bitte in GitHub unter Actions nachsehen.'
        return
    }

    Write-Info "Run-ID: $runId"
    if ($runUrl) {
        Write-Info "URL   : $runUrl"
    }

    $watchArgs = @('run', 'watch', $runId, '--interval', '5')
    if ($Repo) {
        $watchArgs += @('--repo', $Repo)
    }
    Invoke-External -Command 'gh' -Arguments $watchArgs

    Write-Success 'Workflow erfolgreich beendet. Jetzt lokal einmal git pull ausfuehren, damit VERSION/BUILD wieder synchron sind.'
}

function Invoke-LocalMode {
    Require-Command 'git'
    Assert-GitClean

    $currentVersion = Get-CurrentVersion
    $effectiveVersion = Get-NextVersion -CurrentVersion $currentVersion -VersionOverride $Version -BumpMode $Bump
    $effectiveBuild = Get-BuildNumber -RequestedBuildNumber $BuildNumber

    Write-Step 'Lokale Version aktualisieren'
    Set-TextFileNoNewline -Path $VersionFile -Value $effectiveVersion
    Set-TextFileNoNewline -Path $BuildFile -Value $effectiveBuild
    Sync-ExportPresetVersions -Path $ExportPresetsFile -VersionString $effectiveVersion -BuildString $effectiveBuild

    Write-Info "VERSION -> $effectiveVersion"
    Write-Info "BUILD   -> $effectiveBuild"

    if ($Commit) {
        Write-Step 'Git Commit'
        Invoke-Git @('add', 'VERSION', 'BUILD', 'export_presets.cfg')
        $commitMessage = "chore: release $effectiveVersion (build $effectiveBuild)"
        Invoke-Git @('commit', '-m', $commitMessage)
        Write-Success $commitMessage
    }

    if ($Push) {
        Write-Step 'Git Push'
        Invoke-Git @('push', 'origin', 'HEAD')
        Write-Success 'Branch wurde gepusht.'
    }

    if ($Tag) {
        Write-Step 'Git Tag'
        $tagName = "v$effectiveVersion"
        Invoke-Git @('tag', '-f', $tagName)
        Write-Info "Tag gesetzt: $tagName"

        if ($Push) {
            Invoke-Git @('push', 'origin', $tagName, '--force')
            Write-Success 'Tag wurde gepusht.'
        }
    }

    if ($BuildLocal) {
        Write-Step 'Lokales Build'
        Require-Command 'dotnet'
        Invoke-External -Command 'dotnet' -Arguments @('restore', $Solution)
        Invoke-External -Command 'dotnet' -Arguments @('build', $Solution, '-c', 'Release')

        if (-not $GodotExe) {
            throw 'Fuer -BuildLocal wird -GodotExe benoetigt.'
        }
        if (-not (Test-Path -LiteralPath $GodotExe)) {
            throw "GodotExe nicht gefunden: $GodotExe"
        }

        New-Item -ItemType Directory -Force -Path $WindowsDistDir | Out-Null
        New-Item -ItemType Directory -Force -Path $InstallerDistDir | Out-Null

        Invoke-External -Command $GodotExe -Arguments @('--headless', '--path', $ScriptRoot, '--build-solutions', '--export-release', $ExportPreset, (Join-Path $WindowsDistDir 'IFeelDumpQuiz.exe'))

        Copy-Item -LiteralPath $VersionFile -Destination (Join-Path $WindowsDistDir 'VERSION') -Force
        Copy-Item -LiteralPath $BuildFile -Destination (Join-Path $WindowsDistDir 'BUILD') -Force

        if (Test-Path -LiteralPath $PortableZip) {
            Remove-Item -LiteralPath $PortableZip -Force
        }
        Compress-Archive -Path (Join-Path $WindowsDistDir '*') -DestinationPath $PortableZip -Force
        Write-Success "ZIP erstellt: $PortableZip"

        if (-not $SkipInstaller) {
            Require-File -Path $BuildInstallerScript -Label 'build-installer.ps1'
            & $BuildInstallerScript
            if ($LASTEXITCODE -ne 0) {
                throw "Installer-Build fehlgeschlagen (ExitCode $LASTEXITCODE)."
            }
        }
    }

    if ($CreateGitHubRelease) {
        Write-Step 'GitHub Release anlegen'
        Ensure-GitHubCliAuthenticated

        $tagName = "v$effectiveVersion"
        $setupPath = Join-Path $InstallerDistDir ("IFeelDump-Setup-{0}.exe" -f $effectiveVersion)
        $ghArgs = @('release', 'create', $tagName)
        if ($Repo) {
            $ghArgs += @('--repo', $Repo)
        }
        if (Test-Path -LiteralPath $PortableZip) {
            $ghArgs += $PortableZip
        }
        if (Test-Path -LiteralPath $setupPath) {
            $ghArgs += $setupPath
        }
        $ghArgs += @('--title', ("IFeelDump Quiz {0}" -f $tagName))
        $ghArgs += @('--notes', ("Release $effectiveVersion`n`nBuild: $effectiveBuild"))
        if ($PreRelease) {
            $ghArgs += '--prerelease'
        }
        if ($Draft) {
            $ghArgs += '--draft'
        }

        Invoke-External -Command 'gh' -Arguments $ghArgs
        Write-Success 'GitHub Release wurde angelegt.'
    }

    Write-Success 'Lokaler Release-Ablauf abgeschlossen.'
}

Write-Step 'IFeelDump Release Script'
Write-Info "Modus: $Mode"

switch ($Mode) {
    'workflow' { Invoke-WorkflowMode }
    'local'    { Invoke-LocalMode }
    default    { throw "Unbekannter Modus: $Mode" }
}
