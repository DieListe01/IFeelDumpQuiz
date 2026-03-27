# Screen Flow / UI-Zustände

## 1. MainMenu
- Spieltitel
- Button: Lokales Spiel starten
- Button: Spielhistorie
- Button: Beenden

## 2. LocalGameSetup
- Spieleranzahl (1–4)
- Namen der Spieler
- Fragenanzahl
- Kategorie
- Zeit pro Frage
- Button: Starten
- Button: Zurück

## 3. QuizGame
### Bereiche
- Kopfbereich mit Fortschritt, Kategorie, Frage X/Y
- Spielerliste / aktueller Spieler
- Fragetext
- 4 Antwortfelder
- Moderatorbereich

### Zustände
1. PreparingQuestion
2. SelectingStartingPlayer
3. CollectingAnswers
4. ModeratorPause
5. RevealingAnswer
6. TransitionToNextQuestion
7. GameFinished

## 4. Statistics
- Gesamtsieger
- Tabelle aller Spieler
- Punkte
- richtige / falsche Antworten
- Trefferquote
- Button: Zur Historie speichern & anzeigen
- Button: Hauptmenü

## 5. History
- Liste aller bisherigen Spiele
- Filter optional später
- Details pro Spiel
- Einzelnes Spiel löschen
- Alle Spiele löschen
- Zurück
