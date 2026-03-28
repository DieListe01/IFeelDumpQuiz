using Godot;
using IFeelDumpQuiz;
using System.Collections.Generic;
using System.Linq;

public partial class GameSetup : Control
{
    private SpinBox _playerCount = null!;
    private SpinBox _questionCount = null!;
    private SpinBox _timePerQuestion = null!;
    private OptionButton _category = null!;
    private OptionButton _mode = null!;
    private Label _statusLabel = null!;
    private Button _startButton = null!;
    private readonly List<LineEdit> _nameInputs = new();
    private List<QuestionData> _loadedQuestions = new();

    public override void _Ready()
    {
        AppState.EnsureLoaded();

        _playerCount = GetNode<SpinBox>("RootMargin/Center/MainPanel/MainVBox/FormGrid/PlayerCount");
        _questionCount = GetNode<SpinBox>("RootMargin/Center/MainPanel/MainVBox/FormGrid/QuestionCount");
        _timePerQuestion = GetNode<SpinBox>("RootMargin/Center/MainPanel/MainVBox/FormGrid/TimePerQuestion");
        _category = GetNode<OptionButton>("RootMargin/Center/MainPanel/MainVBox/FormGrid/CategorySelect");
        _mode = GetNode<OptionButton>("RootMargin/Center/MainPanel/MainVBox/FormGrid/ModeSelect");
        _statusLabel = GetNode<Label>("RootMargin/Center/MainPanel/MainVBox/StatusLabel");
        _startButton = GetNode<Button>("RootMargin/Center/MainPanel/MainVBox/BottomButtons/BtnStart");

        _nameInputs.Add(GetNode<LineEdit>("RootMargin/Center/MainPanel/MainVBox/PlayersBox/Player1"));
        _nameInputs.Add(GetNode<LineEdit>("RootMargin/Center/MainPanel/MainVBox/PlayersBox/Player2"));
        _nameInputs.Add(GetNode<LineEdit>("RootMargin/Center/MainPanel/MainVBox/PlayersBox/Player3"));
        _nameInputs.Add(GetNode<LineEdit>("RootMargin/Center/MainPanel/MainVBox/PlayersBox/Player4"));

        _startButton.Pressed += OnStartPressed;
        GetNode<Button>("RootMargin/Center/MainPanel/MainVBox/BottomButtons/BtnBack").Pressed += OnBackPressed;
        _playerCount.ValueChanged += _ => RefreshPlayerInputs();

        FillModes();
        ApplySavedSetup();
        ReloadQuestions();
        RefreshPlayerInputs();
    }

    private void FillModes()
    {
        _mode.Clear();
        _mode.AddItem(GameModes.Simultaneous);
        _mode.AddItem(GameModes.TurnBased);
    }

    private void ApplySavedSetup()
    {
        var savedPlayerCount = Mathf.Clamp(AppState.LastGameConfig.PlayerNames.Count == 0 ? 2 : AppState.LastGameConfig.PlayerNames.Count, 1, 4);
        _playerCount.Value = savedPlayerCount;
        _questionCount.Value = Mathf.Max(1, AppState.LastGameConfig.QuestionCount);
        _timePerQuestion.Value = Mathf.Clamp(AppState.LastGameConfig.TimePerQuestionSeconds, 5, 120);
        _mode.Select(AppState.LastGameConfig.AnswerMode == GameModes.TurnBased ? 1 : 0);

        for (var i = 0; i < _nameInputs.Count; i++)
        {
            _nameInputs[i].Text = i < AppState.LastPlayerNames.Count ? AppState.LastPlayerNames[i] : string.Empty;
        }
    }

    private void ReloadQuestions()
    {
        _loadedQuestions = QuestionRepository.LoadQuestions();
        FillCategories();
        RefreshQuestionAvailability();
    }

    private void FillCategories()
    {
        _category.Clear();
        _category.AddItem("Alle");

        var categories = _loadedQuestions
            .Select(question => question.Category)
            .Distinct()
            .OrderBy(category => category)
            .ToList();

        foreach (var category in categories)
        {
            _category.AddItem(category);
        }

        var desiredCategory = AppState.LastGameConfig.Category;
        var desiredIndex = 0;
        for (var i = 0; i < _category.ItemCount; i++)
        {
            if (_category.GetItemText(i) == desiredCategory)
            {
                desiredIndex = i;
                break;
            }
        }

        _category.Select(desiredIndex);
    }

    private void RefreshPlayerInputs()
    {
        var activePlayers = (int)_playerCount.Value;
        for (var i = 0; i < _nameInputs.Count; i++)
        {
            _nameInputs[i].Editable = i < activePlayers;
            _nameInputs[i].PlaceholderText = i < activePlayers ? $"Spieler {i + 1}" : "inaktiv";
            if (i >= activePlayers)
            {
                _nameInputs[i].Text = string.Empty;
            }
        }
    }

    private void RefreshQuestionAvailability()
    {
        if (_loadedQuestions.Count == 0)
        {
            _statusLabel.Text = "Keine Fragen geladen. Bitte im Hauptmenue zuerst 'Fragen verwalten' nutzen.";
            _questionCount.MaxValue = 1;
            _questionCount.Value = 1;
            _category.Disabled = true;
            _startButton.Disabled = true;
            return;
        }

        _statusLabel.Text = $"{_loadedQuestions.Count} Fragen geladen. Letzte Einstellungen wurden uebernommen.";
        _questionCount.MaxValue = _loadedQuestions.Count;
        _questionCount.Value = Mathf.Clamp((int)_questionCount.Value, 1, _loadedQuestions.Count);
        _category.Disabled = false;
        _startButton.Disabled = false;
    }

    private void OnStartPressed()
    {
        if (_loadedQuestions.Count == 0)
        {
            _statusLabel.Text = "Es sind keine spielbaren Fragen vorhanden.";
            return;
        }

        var count = (int)_playerCount.Value;
        var visibleNames = _nameInputs.Take(count)
            .Select((input, index) => string.IsNullOrWhiteSpace(input.Text) ? $"Spieler {index + 1}" : input.Text.Trim())
            .ToList();

        var allNames = _nameInputs.Select(input => input.Text.Trim()).ToList();
        var selectedCategory = _category.GetItemText(_category.Selected);
        var config = new GameConfig
        {
            PlayerNames = visibleNames,
            QuestionCount = (int)_questionCount.Value,
            Category = selectedCategory,
            TimePerQuestionSeconds = (int)_timePerQuestion.Value,
            AnswerMode = _mode.GetItemText(_mode.Selected)
        };

        AppState.SaveSetup(config, allNames);
        GameSession.StartNewGame(config, _loadedQuestions);
        GetTree().ChangeSceneToFile("res://scenes/GameScene.tscn");
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }
}
