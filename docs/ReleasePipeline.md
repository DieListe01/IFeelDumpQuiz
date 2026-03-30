# Release Pipeline

Diese Pipeline bildet den kompletten GitHub-Ablauf fuer **IFeelDump Quiz** ab.

## Enthaltene Workflows

### 1. CI
Datei: `.github/workflows/ci.yml`

Laeuft bei:
- Push auf `main`, `master`, `develop`, `dev`
- Push auf `feature/**`, `fix/**`, `hotfix/**`
- Pull Requests gegen die Haupt-Branches
- manuellem Start

Der Workflow prueft:
- Pflichtdateien vorhanden
- `VERSION` im Format `Major.Minor.Patch`
- Export-Preset `Windows Desktop` vorhanden
- `dotnet restore`
- `dotnet build`
- optionale Testprojekte, falls vorhanden

Zusaetzlich wird ein `build.binlog` als Artefakt gespeichert.

### 2. Windows Release
Datei: `.github/workflows/windows-release.yml`

Laeuft bei:
- Push auf Tags wie `v0.2.5`
- manuellem Start ueber **Actions -> Windows Release -> Run workflow**

Der Workflow erledigt:
1. Version aufloesen
2. `VERSION` und `BUILD` schreiben
3. bei manuellem Start die neue `VERSION` committen und einen Tag setzen
4. Godot .NET + Export Templates herunterladen
5. C#-Loesung bauen
6. Godot-Windows-Export erzeugen
7. Portable ZIP bauen
8. Installer mit Inno Setup bauen
9. GitHub Release mit ZIP + Setup.exe erstellen oder aktualisieren

## Empfohlener Ablauf

### Normaler Entwicklungsablauf
1. Branch erstellen
2. lokal entwickeln
3. pushen
4. Pull Request erstellen
5. CI muss gruen sein
6. nach `main` mergen

### Release erstellen
Es gibt zwei Wege:

#### Variante A: manuell in GitHub
1. **Actions** oeffnen
2. **Windows Release** waehlen
3. **Run workflow** klicken
4. optional `version` angeben oder `bump` waehlen
5. Workflow erstellt Commit, Tag, ZIP, Installer und GitHub Release

#### Variante B: per Tag
1. lokal auf `main` wechseln
2. Tag setzen, z. B. `v0.2.5`
3. Tag pushen
4. Release-Workflow startet automatisch

Beispiel:

```bash
git checkout main
git pull
git tag v0.2.5
git push origin v0.2.5
```

## Wichtige Dateien

- `VERSION` -> sichtbare App-Version
- `BUILD` -> Buildnummer aus GitHub Actions
- `export_presets.cfg` -> Godot Export-Konfiguration
- `scripts/release/Resolve-Version.ps1` -> Versionsermittlung
- `scripts/release/Sync-ExportVersions.ps1` -> Windows-Dateiversion im Export-Preset

## Ergebnis pro Release

Der Release-Workflow erzeugt:
- `dist/IFeelDumpQuiz-win64.zip`
- `dist/installer/IFeelDump-Setup-<VERSION>.exe`
- GitHub Release mit Tag `v<VERSION>`

## Hinweise

- Der Workflow verwendet `windows-2022`, weil sowohl Godot-Windows-Export als auch Inno Setup gebraucht werden.
- `export_presets.cfg` muss das Preset `Windows Desktop` enthalten.
- Der Godot-CLI-Export erwartet einen existierenden Zielordner und einen Dateinamen fuer die EXE.
- Workflow-Dateien muessen auf dem Default-Branch vorhanden sein, damit die Events zuverlaessig greifen.
