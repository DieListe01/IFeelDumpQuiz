# IFeelDump Quiz

Modernes lokales Quizspiel in Godot .NET mit Quizshow-Atmosphaere, CSV-Fragen, Spielhistorie und Windows-Updatefunktion ueber GitHub Releases.

## Highlights

- lokales Quiz fuer 1 bis 4 Spieler
- Moderator-gesteuerter Ablauf
- Fragen aus CSV
- Spielhistorie in SQLite
- Windows-Installer mit Inno Setup
- Update-Pruefung gegen GitHub Releases

## Technik

- Godot 4.6.1 .NET
- C# / .NET 8
- SQLite via `Microsoft.Data.Sqlite`
- GitHub Actions fuer Windows-Release-ZIP

## Wichtige Dateien

- `VERSION` - zentrale Versionsnummer
- `data/questions.csv` - Fragenquelle
- `ui/QuizTheme.tres` - globales Theme
- `installer/IFeelDumpQuiz.iss` - Windows-Installer
- `.github/workflows/windows-release.yml` - Release-Workflow
- `docs/ReleaseAndUpdate.md` - Build- und Updateablauf
- `docs/FirstReleaseChecklist.md` - erste Release-Checkliste

## Lokaler Start

1. Projekt in Godot 4.6.1 .NET oeffnen
2. NuGet-Abhaengigkeiten wiederherstellen
3. Projekt starten

## Fragenformat

Die Fragen liegen in `data/questions.csv`.

```csv
question;answer_a;answer_b;answer_c;answer_d;correct;category;explanation
Was ist die Hauptstadt von Frankreich?;Berlin;Madrid;Paris;Rom;2;Geographie;Paris ist die Hauptstadt von Frankreich.
```

`correct` nutzt `0-3`.

## Windows Release

1. Version in `VERSION` anpassen
2. Windows-Export erstellen
3. ZIP `IFeelDumpQuiz-win64.zip` erzeugen
4. optional Installer mit Inno Setup bauen
5. GitHub Release mit Tag wie `v0.1.0` veroeffentlichen

## Update-System

- Die App prueft im Hauptmenue auf eine neue Version
- Quelle: GitHub Releases von `DieListe01/IFeelDumpQuiz`
- Bei Zustimmung wird das ZIP geladen, die App beendet und aktualisiert

## Status

Das Projekt ist bereits spielbar und besitzt eine Release-/Update-Basis fuer Windows. Feinschliff bei UI, Effekten und Release-Prozess ist weiterhin moeglich.
