using Godot;
using IFeelDumpQuiz;
using IFeelDumpQuiz.Services;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class HistoryScene : Control
{
    private readonly StatsRepository _repository = StatsRepository.CreateDefault();
    private readonly List<GameHistoryEntry> _historyEntries = new();
    private ItemList _gamesList = null!;
    private RichTextLabel _detailsText = null!;
    private Label _statusLabel = null!;

    public override void _Ready()
    {
        _gamesList = GetNode<ItemList>("RootMargin/MainPanel/MainVBox/ContentRow/GamesList");
        _detailsText = GetNode<RichTextLabel>("RootMargin/MainPanel/MainVBox/ContentRow/DetailsPanel/DetailsText");
        _statusLabel = GetNode<Label>("RootMargin/MainPanel/MainVBox/Status");

        GetNode<Button>("RootMargin/MainPanel/MainVBox/ButtonRow/BtnDelete").Pressed += OnDeletePressed;
        GetNode<Button>("RootMargin/MainPanel/MainVBox/ButtonRow/BtnDeleteAll").Pressed += OnDeleteAllPressed;
        GetNode<Button>("RootMargin/MainPanel/MainVBox/ButtonRow/BtnBack").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        _gamesList.ItemSelected += OnGameSelected;

        ReloadHistory();
    }

    private void ReloadHistory()
    {
        _historyEntries.Clear();
        _historyEntries.AddRange(_repository.GetGameHistory());

        _gamesList.Clear();
        foreach (var entry in _historyEntries)
        {
            var players = entry.Players.Count > 0
                ? string.Join(", ", entry.Players.Select(player => player.Name))
                : "ohne Spieler";
            _gamesList.AddItem($"{entry.CreatedAt:dd.MM.yyyy HH:mm}  |  {entry.Category}  |  {players}");
        }

        if (_historyEntries.Count == 0)
        {
            _statusLabel.Text = "Noch keine Spiele gespeichert.";
            _detailsText.Clear();
            _detailsText.AppendText("Sobald ein Spiel abgeschlossen ist, erscheint es hier in der Historie.");
            return;
        }

        _statusLabel.Text = $"{_historyEntries.Count} Spiel(e) gespeichert.";
        _gamesList.Select(0);
        ShowDetails(0);
    }

    private void OnGameSelected(long index)
    {
        ShowDetails((int)index);
    }

    private void ShowDetails(int index)
    {
        if (index < 0 || index >= _historyEntries.Count)
        {
            return;
        }

        var entry = _historyEntries[index];
        _detailsText.Clear();
        _detailsText.AppendText($"Spiel vom {entry.CreatedAt:dd.MM.yyyy} um {entry.CreatedAt:HH:mm:ss}\n\n");
        _detailsText.AppendText($"Kategorie: {entry.Category}\n");
        _detailsText.AppendText($"Fragen: {entry.QuestionCount}\n");
        _detailsText.AppendText($"Zeit pro Spieler: {entry.TimePerQuestionSeconds}s\n");
        _detailsText.AppendText($"Gesamtdauer: {TimeSpan.FromSeconds(entry.DurationSeconds):mm\\:ss}\n\n");
        _detailsText.AppendText("Punktestand:\n");

        foreach (var player in entry.Players.OrderByDescending(player => player.Score).ThenBy(player => player.Name))
        {
            _detailsText.AppendText($"- {player.Name}: {player.Score} Punkte | Richtig: {player.CorrectAnswers} | Falsch: {player.WrongAnswers} | Trefferquote: {player.Accuracy:0.0}%\n");
        }
    }

    private void OnDeletePressed()
    {
        var selectedItems = _gamesList.GetSelectedItems();
        if (selectedItems.Length == 0)
        {
            _statusLabel.Text = "Bitte zuerst ein Spiel auswaehlen.";
            return;
        }

        var selectedIndex = selectedItems[0];
        if (selectedIndex < 0 || selectedIndex >= _historyEntries.Count)
        {
            return;
        }

        _repository.DeleteGame(_historyEntries[selectedIndex].Id);
        ReloadHistory();
        _statusLabel.Text = "Spiel wurde geloescht.";
    }

    private void OnDeleteAllPressed()
    {
        _repository.DeleteAllGames();
        ReloadHistory();
        _statusLabel.Text = "Alle gespeicherten Spiele wurden geloescht.";
    }
}
