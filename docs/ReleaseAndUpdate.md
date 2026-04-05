# Release und Update

## Repository

- GitHub Repository: `https://github.com/DieListe01/IFeelDumpQuiz`
- Die App prueft beim Start `latest release` gegen die GitHub Releases API.
- Erwartetes Windows-Asset fuer Updates: `IFeelDumpQuiz-win64.zip`
- Die Versionsnummer liegt zentral in `VERSION`.

## Windows Build

1. In Godot das Preset `Windows Desktop` verwenden.
2. Export nach `dist/windows/IFeelDumpQuiz.exe`.
3. Den kompletten Export-Inhalt zusaetzlich als ZIP packen:
   - Dateiname: `IFeelDumpQuiz-win64.zip`
4. ZIP als Release-Asset auf GitHub hochladen.

## Installer bauen

1. Inno Setup installieren.
2. `build-installer.ps1` starten.
3. Ergebnis liegt in `dist/installer/IFeelDump-Setup-<VERSION>.exe`.
4. Der Installer kann laufende Instanzen von `IFeelDumpQuiz.exe` automatisch schliessen.

## GitHub Actions

- CI: `.github/workflows/ci.yml`
- Release: `.github/workflows/windows-release.yml`
- Beide Workflows laufen auf `windows-2022`.
- Der Release-Workflow kann manuell gestartet werden oder automatisch bei Tags wie `v0.2.5` laufen.
- Der Workflow installiert Godot 4.6.1 Mono inklusive Export Templates automatisch.
- Danach folgen `dotnet restore`, `dotnet build`, Windows-Export, ZIP-Erzeugung und Inno-Setup-Build.
- Das GitHub Release bekommt diese Assets:
  - `IFeelDumpQuiz-win64.zip`
  - `IFeelDump-Setup-<VERSION>.exe`

## Update-Ablauf in der App

1. App startet.
2. `MainMenu` prueft GitHub Releases auf eine neuere Version.
3. Wenn eine neue Version gefunden wird, fragt die App den Benutzer.
4. Bei Zustimmung wird `IFeelDumpQuiz-win64.zip` heruntergeladen.
5. Ein PowerShell-Updater wartet auf das Beenden der App.
6. Dateien werden ins Installationsverzeichnis kopiert.
7. Die neue EXE wird automatisch neu gestartet.

## End-to-End Test fuer Setup und Update

1. Release bauen, sodass diese Dateien vorliegen:
   - `IFeelDumpQuiz-win64.zip`
   - `IFeelDump-Setup-<VERSION>.exe`
2. Setup ausfuehren und die App installieren.
3. Die installierte App einmal starten und pruefen, ob sie normal ins Hauptmenue kommt.
4. Fuer einen Update-Test eine neuere Version als GitHub Release veroeffentlichen.
5. Die installierte App erneut starten.
6. Die App muss beim Start automatisch auf GitHub pruefen.
7. Bei bestaetigtem Update laeuft der Download im Hintergrund.
8. Danach schliesst sich die App, ersetzt ihre Dateien und startet mit der neuen Version neu.
9. Abschliessend pruefen:
   - neue Versionsanzeige im Hauptmenue
   - App startet weiterhin normal
   - `VERSION` im Installationsordner entspricht dem neuen Release

## Wichtige Hinweise

- Die Update-Funktion ist fuer Windows ausgelegt.
- Das Installationsverzeichnis ist das Verzeichnis der laufenden EXE.
- Fuer funktionierende Updates muss das Release-ZIP dieselbe Ordnerstruktur wie der Windows-Export enthalten.
