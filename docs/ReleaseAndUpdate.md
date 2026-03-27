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
2. `installer/IFeelDumpQuiz.iss` oeffnen.
3. Installer kompilieren.
4. Ergebnis liegt in `dist/installer/IFeelDumpQuiz-Setup.exe`.
5. Der Installer kann laufende Instanzen von `IFeelDumpQuiz.exe` automatisch schliessen.

## GitHub Actions

- Workflow-Datei: `.github/workflows/windows-release.yml`
- Der Workflow liest die Version aus `VERSION`.
- Der Workflow installiert Godot 4.6.1 inklusive Export-Templates automatisch.
- Der Workflow baut zuerst das C#-Projekt und fuehrt dann den Windows-Export per Godot CLI aus.
- Der Workflow erstellt aus `dist/windows` das Release-ZIP `IFeelDumpQuiz-win64.zip`.
- Bei Tags wie `v0.1.0` wird das ZIP direkt an den GitHub Release angehaengt.
- Damit ist der Windows-Build in GitHub Actions deutlich automatisierter.

## Update-Ablauf in der App

1. App startet.
2. `MainMenu` prueft GitHub Releases auf eine neuere Version.
3. Wenn eine neue Version gefunden wird, fragt die App den Benutzer.
4. Bei Zustimmung wird `IFeelDumpQuiz-win64.zip` heruntergeladen.
5. Ein PowerShell-Updater wartet auf das Beenden der App.
6. Dateien werden ins Installationsverzeichnis kopiert.
7. Die neue EXE wird automatisch neu gestartet.

## Wichtige Hinweise

- Die Update-Funktion ist fuer Windows ausgelegt.
- Das Installationsverzeichnis ist das Verzeichnis der laufenden EXE.
- Fuer funktionierende Updates muss das Release-ZIP dieselbe Ordnerstruktur wie der Windows-Export enthalten.
