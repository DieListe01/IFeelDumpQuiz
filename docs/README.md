# IFeelDump Quiz – sauberes Godot-.NET-Startprojekt

Dieses ZIP ist als **eigenständiges Projekt** gedacht.

## Enthalten
- lauffähige Godot-4.6-.NET-Projektstruktur
- valides `project.godot`
- saubere `.tscn`-Szenen
- C#-Grundlogik für:
  - Hauptmenü
  - lokales Spiel-Setup
  - Quizrunde
  - Statistik
  - Historie (Platzhalter)
- Beispiel-Fragen als JSON

## Aktueller Funktionsstand
- Startmenü mit "Lokales Spiel starten"
- Setup:
  - 1–4 Spieler
  - Spielernamen
  - Anzahl Fragen
  - Kategorie
  - Zeit pro Frage
- Spielrunde:
  - Startspieler wird per Shuffle-Liste bestimmt
  - alle Spieler antworten nacheinander auf dieselbe Frage
  - Moderator kann auflösen
  - richtige Antwort + korrekte Spieler werden angezeigt
  - danach nächste Frage / zur Statistik
- Statistik:
  - Punkte pro Spieler
  - richtige / falsche Antworten
  - Trefferquote
- Historie:
  - aktuell nur Platzhalter-Scene
  - DB-Schema liegt unter `docs/sqlite_schema.sql`

## Noch offen
- echte SQLite-Anbindung
- Persistierung der Spielhistorie
- besseres UI-Styling / Animationen / Sounds
- echter Moderator-Kommentarbereich

## Einstieg
1. Projekt in Godot 4.6.1 mono öffnen
2. Build / Restore der C#-Abhängigkeiten abwarten
3. `MainMenu.tscn` ist die Startszene

## Steuerung in der Quizrunde
- Antwort per Mausklick auf die 4 Buttons
- alternativ Tastatur:
  - `1` / `2` / `3` / `4`
  - `A` / `B` / `C` / `D`

## Architekturhinweis
Das Projekt ist bewusst einfach gehalten, damit du es leicht in dein größeres IFeelDump-Konzept überführen kannst.
