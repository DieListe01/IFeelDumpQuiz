using Godot;
using IFeelDumpQuiz;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameScene : Control
{
    private enum AnswerVisualState
    {
        Neutral,
        Locked,
        Correct,
        Wrong
    }

    private Label _questionCounter = null!;
    private Label _categoryLabel = null!;
    private Label _phaseLabel = null!;
    private Label _currentPlayerLabel = null!;
    private PanelContainer _currentPlayerBanner = null!;
    private Label _answerHintLabel = null!;
    private Label _keyboardHintLabel = null!;
    private Label _shuffleLabel = null!;
    private Label _turnOrderLabel = null!;
    private Label _timerLabel = null!;
    private Label _resultLabel = null!;
    private Label _subInfoLabel = null!;
    private Label _questionText = null!;
    private RichTextLabel _scoreboard = null!;
    private Button _btnResolve = null!;
    private Button _btnNext = null!;
    private readonly List<Button> _answerButtons = new();
    private readonly Dictionary<Button, Vector2> _answerBaseScales = new();
    private Godot.Timer _questionTimer = null!;
    private Godot.Timer _shuffleTimer = null!;
    private AudioStreamPlayer _countdownPlayer = null!;
    private AudioStreamGeneratorPlayback? _countdownPlayback;

    private readonly Dictionary<string, int> _answersByPlayer = new();
    private List<int> _turnOrder = new();
    private int _turnIndex;
    private int _shuffleStep;
    private int _shuffleCyclesRemaining;
    private int _lastBeepSecond = -1;
    private bool _isResolved;
    private bool _isShuffleRunning;
    private QuestionData? _question;

    public override void _Ready()
    {
        _questionCounter = GetNode<Label>("RootMargin/MainVBox/TopPanel/TopVBox/HeaderRow/QuestionCounter");
        _categoryLabel = GetNode<Label>("RootMargin/MainVBox/TopPanel/TopVBox/HeaderRow/CategoryLabel");
        _phaseLabel = GetNode<Label>("RootMargin/MainVBox/TopPanel/TopVBox/HeaderRow/PhaseLabel");
        _timerLabel = GetNode<Label>("RootMargin/MainVBox/TopPanel/TopVBox/HeaderRow/TimerLabel");
        _subInfoLabel = GetNode<Label>("RootMargin/MainVBox/TopPanel/TopVBox/SubInfoLabel");
        _shuffleLabel = GetNode<Label>("RootMargin/MainVBox/QuestionPanel/QuestionVBox/ShuffleLabel");
        _questionText = GetNode<Label>("RootMargin/MainVBox/QuestionPanel/QuestionVBox/QuestionText");
        _answerHintLabel = GetNode<Label>("RootMargin/MainVBox/QuestionPanel/QuestionVBox/AnswerHintLabel");
        _keyboardHintLabel = GetNode<Label>("RootMargin/MainVBox/QuestionPanel/QuestionVBox/KeyboardHintLabel");
        _currentPlayerBanner = GetNode<PanelContainer>("RootMargin/MainVBox/QuestionPanel/QuestionVBox/CurrentPlayerBanner");
        _currentPlayerLabel = GetNode<Label>("RootMargin/MainVBox/QuestionPanel/QuestionVBox/CurrentPlayerBanner/CurrentPlayerLabel");
        _turnOrderLabel = GetNode<Label>("RootMargin/MainVBox/QuestionPanel/QuestionVBox/TurnOrderLabel");
        _resultLabel = GetNode<Label>("RootMargin/MainVBox/BottomRow/ModeratorPanel/ModeratorVBox/ResultLabel");
        _scoreboard = GetNode<RichTextLabel>("RootMargin/MainVBox/BottomRow/ScorePanel/ScoreVBox/Scoreboard");
        _btnResolve = GetNode<Button>("RootMargin/MainVBox/BottomRow/ModeratorPanel/ModeratorVBox/ModeratorRow/BtnResolve");
        _btnNext = GetNode<Button>("RootMargin/MainVBox/BottomRow/ModeratorPanel/ModeratorVBox/ModeratorRow/BtnNext");
        _questionTimer = GetNode<Godot.Timer>("QuestionTimer");
        _shuffleTimer = GetNode<Godot.Timer>("ShuffleTimer");
        _countdownPlayer = GetNode<AudioStreamPlayer>("CountdownPlayer");

        _answerButtons.Add(GetNode<Button>("RootMargin/MainVBox/AnswersGrid/Answer1"));
        _answerButtons.Add(GetNode<Button>("RootMargin/MainVBox/AnswersGrid/Answer2"));
        _answerButtons.Add(GetNode<Button>("RootMargin/MainVBox/AnswersGrid/Answer3"));
        _answerButtons.Add(GetNode<Button>("RootMargin/MainVBox/AnswersGrid/Answer4"));

        for (var i = 0; i < _answerButtons.Count; i++)
        {
            var answerIndex = i;
            _answerButtons[i].Pressed += () => OnAnswerPressed(answerIndex);
            _answerButtons[i].MouseEntered += () => OnAnswerHoverChanged(_answerButtons[i], true);
            _answerButtons[i].MouseExited += () => OnAnswerHoverChanged(_answerButtons[i], false);
            _answerButtons[i].FocusEntered += () => OnAnswerHoverChanged(_answerButtons[i], true);
            _answerButtons[i].FocusExited += () => OnAnswerHoverChanged(_answerButtons[i], false);
            _answerBaseScales[_answerButtons[i]] = _answerButtons[i].Scale;
        }

        _btnResolve.Pressed += OnResolvePressed;
        _btnNext.Pressed += OnNextPressed;
        GetNode<Button>("RootMargin/MainVBox/BottomRow/ModeratorPanel/ModeratorVBox/ModeratorRow/BtnAbort").Pressed += OnAbortPressed;
        _questionTimer.Timeout += OnQuestionTimeout;
        _shuffleTimer.Timeout += OnShuffleTick;

        ConfigureCountdownAudio();
        LoadCurrentQuestion();
    }

    public override void _Process(double delta)
    {
        if (!_questionTimer.IsStopped())
        {
            var currentPlayerName = _turnIndex < _turnOrder.Count && !_isShuffleRunning
                ? GameSession.Players[_turnOrder[_turnIndex]].Name
                : "bereit";
            var secondsLeft = Mathf.CeilToInt((float)_questionTimer.TimeLeft);
            _timerLabel.Text = $"{currentPlayerName} · {secondsLeft}s";
            TryPlayCountdownBeep(secondsLeft);
        }
        else if (_isShuffleRunning)
        {
            _timerLabel.Text = "Auslosung";
        }
        else
        {
            _lastBeepSecond = -1;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_isResolved || _isShuffleRunning)
        {
            return;
        }

        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
        {
            return;
        }

        switch (keyEvent.Keycode)
        {
            case Key.Key1:
            case Key.Kp1:
                OnAnswerPressed(0);
                break;
            case Key.Key2:
            case Key.Kp2:
                OnAnswerPressed(1);
                break;
            case Key.Key3:
            case Key.Kp3:
                OnAnswerPressed(2);
                break;
            case Key.Key4:
            case Key.Kp4:
                OnAnswerPressed(3);
                break;
        }
    }

    private void LoadCurrentQuestion()
    {
        _question = GameSession.CurrentQuestion;
        if (_question == null)
        {
            GameSession.MarkFinished();
            GetTree().ChangeSceneToFile("res://scenes/ResultScene.tscn");
            return;
        }

        _answersByPlayer.Clear();
        _isResolved = false;
        _isShuffleRunning = false;
        _turnIndex = 0;
        _questionTimer.Stop();
        _shuffleTimer.Stop();
        _turnOrder = GameSession.GetQuestionOrder(GameSession.GetNextStartPlayerIndex()).ToList();

        _questionCounter.Text = $"Frage {GameSession.CurrentQuestionIndex + 1} / {GameSession.ActiveQuestions.Count}";
        _categoryLabel.Text = _question.Category;
        _phaseLabel.Text = "Frage";
        _timerLabel.Text = $"{GameSession.Config.TimePerQuestionSeconds}s pro Spieler";
        _subInfoLabel.Text = "Verdeckt bis zur Aufloesung.";
        _questionText.Text = _question.Text;
        _answerHintLabel.Text = "Startspieler wird ausgelost.";
        _keyboardHintLabel.Text = "Tasten: 1  2  3  4";
        _resultLabel.Text = "Nach allen Antworten folgt die Aufloesung.";
        _shuffleLabel.Text = "Startspieler wird ausgelost ...";
        _currentPlayerLabel.Text = "AM ZUG: wird ausgelost";

        for (var i = 0; i < _answerButtons.Count; i++)
        {
            _answerButtons[i].Text = _question.Answers[i];
            _answerButtons[i].Modulate = Colors.White;
            _answerButtons[i].Disabled = true;
            ApplyAnswerVisualState(i, AnswerVisualState.Neutral);
        }

        RefreshTurnOrderLabel();
        RefreshScoreboard();
        UpdateModeratorButtons();
        StartShuffleAnimation();
    }

    private void StartShuffleAnimation()
    {
        _isShuffleRunning = true;
        _shuffleStep = 0;
        _shuffleCyclesRemaining = GameSession.Players.Count * 4 + 6;
        _phaseLabel.Text = "Auslosung";
        _currentPlayerLabel.Text = "AM ZUG: Auswahl laeuft";
        _answerHintLabel.Text = "Bitte kurz warten.";
        PulseCurrentPlayerBanner();
        _shuffleTimer.Start();
    }

    private void OnShuffleTick()
    {
        if (_turnOrder.Count == 0)
        {
            return;
        }

        var player = GameSession.Players[_turnOrder[_shuffleStep % _turnOrder.Count]];
        _shuffleLabel.Text = $">>> {player.Name} <<<";
        _shuffleStep++;
        _shuffleCyclesRemaining--;

        if (_shuffleCyclesRemaining > 0)
        {
            return;
        }

        _shuffleTimer.Stop();
        _isShuffleRunning = false;
        _phaseLabel.Text = "Antworten";
        _shuffleLabel.Text = $"Startspieler: {GameSession.Players[_turnOrder[0]].Name}";
        _subInfoLabel.Text = "Jeder Spieler hat die volle Zeit.";
        SetAnswerButtonsEnabled(true);
        StartCurrentPlayerTurn();
        RefreshTurnOrderLabel();
        UpdateModeratorButtons();
    }

    private void StartCurrentPlayerTurn()
    {
        if (_turnIndex >= _turnOrder.Count)
        {
            return;
        }

        _questionTimer.Stop();
        _questionTimer.WaitTime = GameSession.Config.TimePerQuestionSeconds;
        _questionTimer.Start();

        var player = GameSession.Players[_turnOrder[_turnIndex]];
        _currentPlayerLabel.Text = $"AM ZUG: {player.Name}";
        _answerHintLabel.Text = $"{player.Name} waehlt jetzt eine Antwort.";
        PulseCurrentPlayerBanner();
        for (var i = 0; i < _answerButtons.Count; i++)
        {
            ApplyAnswerVisualState(i, AnswerVisualState.Neutral);
        }
    }

    private void OnAnswerPressed(int answerIndex)
    {
        if (_question == null || _isResolved || _isShuffleRunning || _turnIndex >= _turnOrder.Count)
        {
            return;
        }

        var player = GameSession.Players[_turnOrder[_turnIndex]];
        if (_answersByPlayer.ContainsKey(player.Name))
        {
            return;
        }

        _answersByPlayer[player.Name] = answerIndex;
        _shuffleLabel.Text = $"Antwort von {player.Name} gespeichert";
        _turnIndex++;

        if (_turnIndex >= _turnOrder.Count)
        {
            _phaseLabel.Text = "Moderation";
            _currentPlayerLabel.Text = "AUFLOESUNG DURCH MODERATOR";
            _answerHintLabel.Text = "Alle Antworten liegen vor.";
            _questionTimer.Stop();
            SetAnswerButtonsEnabled(false);
            SetAllAnswersLocked();
            PulseCurrentPlayerBanner();
        }
        else
        {
            StartCurrentPlayerTurn();
        }

        RefreshTurnOrderLabel();
        UpdateModeratorButtons();
    }

    private void OnQuestionTimeout()
    {
        if (_question == null || _isResolved || _turnIndex >= _turnOrder.Count)
        {
            return;
        }

        var player = GameSession.Players[_turnOrder[_turnIndex]];
        _answersByPlayer[player.Name] = -1;
        _shuffleLabel.Text = $"Zeit fuer {player.Name} abgelaufen";
        _turnIndex++;

        if (_turnIndex >= _turnOrder.Count)
        {
            _phaseLabel.Text = "Moderation";
            _currentPlayerLabel.Text = "AUFLOESUNG DURCH MODERATOR";
            _answerHintLabel.Text = "Zeit abgelaufen.";
            _questionTimer.Stop();
            SetAnswerButtonsEnabled(false);
            SetAllAnswersLocked();
            PulseCurrentPlayerBanner();
        }
        else
        {
            StartCurrentPlayerTurn();
        }

        RefreshTurnOrderLabel();
        UpdateModeratorButtons();
    }

    private void OnResolvePressed()
    {
        if (_question == null || _isResolved || _answersByPlayer.Count < GameSession.Players.Count)
        {
            return;
        }

        _isResolved = true;
        _questionTimer.Stop();
        _phaseLabel.Text = "Aufloesung";
        _shuffleLabel.Text = "Richtige Antwort wird angezeigt";
        _subInfoLabel.Text = "Punkte wurden vergeben.";
        _answerHintLabel.Text = "Moderator entscheidet ueber den naechsten Schritt.";

        var correctPlayers = new List<string>();
        var wrongPlayers = new List<string>();
        foreach (var player in GameSession.Players)
        {
            var selectedIndex = _answersByPlayer.GetValueOrDefault(player.Name, -1);
            var isCorrect = selectedIndex == _question.CorrectIndex;
            GameSession.RecordAnswer(player.Name, _question.Id, selectedIndex, isCorrect);
            if (isCorrect)
            {
                correctPlayers.Add(player.Name);
            }
            else
            {
                wrongPlayers.Add(player.Name);
            }
        }

        for (var i = 0; i < _answerButtons.Count; i++)
        {
            _answerButtons[i].Disabled = true;
            ApplyAnswerVisualState(i, i == _question.CorrectIndex ? AnswerVisualState.Correct : AnswerVisualState.Wrong);
            _answerButtons[i].Text = i == _question.CorrectIndex ? $"Richtige Antwort: {_question.Answers[i]}" : _question.Answers[i];
        }

        var correctPlayerText = correctPlayers.Count > 0 ? string.Join(", ", correctPlayers) : "niemand";
        var wrongPlayerText = wrongPlayers.Count > 0 ? string.Join(", ", wrongPlayers) : "niemand";
        _resultLabel.Text = $"Richtige Antwort: {_question.Answers[_question.CorrectIndex]}\nRichtig: {correctPlayerText}\nFalsch: {wrongPlayerText}\n\nErklaerung: {_question.Explanation}";
        _currentPlayerLabel.Text = "AUFLOESUNG DURCH MODERATOR";
        PulseCurrentPlayerBanner();
        RefreshScoreboard();
        UpdateModeratorButtons();
    }

    private void OnNextPressed()
    {
        if (!_isResolved)
        {
            return;
        }

        GameSession.MoveToNextQuestion();
        if (!GameSession.HasMoreQuestions)
        {
            GameSession.MarkFinished();
            GetTree().ChangeSceneToFile("res://scenes/ResultScene.tscn");
            return;
        }

        LoadCurrentQuestion();
    }

    private void OnAbortPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }

    private void RefreshTurnOrderLabel()
    {
        var parts = new List<string>();
        for (var i = 0; i < _turnOrder.Count; i++)
        {
            var player = GameSession.Players[_turnOrder[i]].Name;
            var marker = i < _turnIndex ? "[x]" : i == _turnIndex && !_isResolved && !_isShuffleRunning ? "[>]" : "[ ]";
            parts.Add($"{marker} {player}");
        }

        _turnOrderLabel.Text = $"Reihenfolge: {string.Join("  |  ", parts)}";
    }

    private void RefreshScoreboard()
    {
        _scoreboard.Clear();
        foreach (var player in GameSession.Players.OrderByDescending(player => player.Score).ThenBy(player => player.Name))
        {
            _scoreboard.AppendText($"{player.Name}: {player.Score} Punkte | Richtig: {player.CorrectAnswers} | Falsch: {player.WrongAnswers}\n");
        }
    }

    private void UpdateModeratorButtons()
    {
        _btnResolve.Disabled = _isResolved || _isShuffleRunning || _answersByPlayer.Count < GameSession.Players.Count;
        _btnNext.Disabled = !_isResolved;
        _btnNext.Text = GameSession.CurrentQuestionIndex + 1 >= GameSession.ActiveQuestions.Count ? "Zur Statistik" : "Naechste Frage";
    }

    private void SetAnswerButtonsEnabled(bool enabled)
    {
        foreach (var button in _answerButtons)
        {
            button.Disabled = !enabled;
            button.FocusMode = enabled ? Control.FocusModeEnum.Click : Control.FocusModeEnum.None;
        }
    }

    private void SetAllAnswersLocked()
    {
        for (var i = 0; i < _answerButtons.Count; i++)
        {
            ApplyAnswerVisualState(i, AnswerVisualState.Locked);
        }
    }

    private void ApplyAnswerVisualState(int index, AnswerVisualState state)
    {
        var button = _answerButtons[index];
        button.Modulate = Colors.White;
        var style = CreateAnswerStyle(state);
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", state == AnswerVisualState.Neutral ? CreateAnswerStyle(AnswerVisualState.Locked, true) : style);
        button.AddThemeStyleboxOverride("pressed", state == AnswerVisualState.Neutral ? CreateAnswerStyle(AnswerVisualState.Locked) : style);
        button.AddThemeStyleboxOverride("focus", state == AnswerVisualState.Neutral ? CreateAnswerStyle(AnswerVisualState.Locked, true) : style);
        button.AddThemeStyleboxOverride("disabled", style);

        if (state == AnswerVisualState.Correct)
        {
            AnimateAnswerButton(button, new Vector2(1.02f, 1.02f), 0.18f);
        }
        else if (state == AnswerVisualState.Wrong)
        {
            AnimateAnswerButton(button, new Vector2(0.985f, 0.985f), 0.16f);
        }
        else
        {
            AnimateAnswerButton(button, Vector2.One, 0.14f);
        }
    }

    private void OnAnswerHoverChanged(Button button, bool isHovered)
    {
        if (button.Disabled || _isResolved || _isShuffleRunning)
        {
            return;
        }

        AnimateAnswerButton(button, isHovered ? new Vector2(1.015f, 1.015f) : Vector2.One, 0.12f);
    }

    private void AnimateAnswerButton(Button button, Vector2 targetScale, double duration)
    {
        button.PivotOffset = button.Size / 2f;
        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(button, "scale", targetScale, duration);
    }

    private void PulseCurrentPlayerBanner()
    {
        _currentPlayerBanner.Modulate = new Color(1f, 1f, 1f, 0.88f);
        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(_currentPlayerBanner, "modulate", Colors.White, 0.22f);
    }

    private void TryPlayCountdownBeep(int secondsLeft)
    {
        if (secondsLeft <= 0 || secondsLeft > 5)
        {
            return;
        }

        if (_lastBeepSecond == secondsLeft)
        {
            return;
        }

        _lastBeepSecond = secondsLeft;
        PlayCountdownTone(secondsLeft == 1 ? 980f : 740f);
    }

    private void ConfigureCountdownAudio()
    {
        var generator = new AudioStreamGenerator
        {
            MixRate = 44100,
            BufferLength = 0.08f
        };

        _countdownPlayer.Stream = generator;
        _countdownPlayer.Play();
        _countdownPlayback = _countdownPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
    }

    private void PlayCountdownTone(float frequency)
    {
        if (_countdownPlayback == null)
        {
            return;
        }

        var sampleRate = 44100f;
        var sampleCount = (int)(sampleRate * 0.08f);
        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / sampleRate;
            var envelope = 1f - (i / (float)sampleCount);
            var sample = Mathf.Sin(2f * Mathf.Pi * frequency * t) * 0.18f * envelope;
            _countdownPlayback.PushFrame(new Vector2(sample, sample));
        }
    }

    private static StyleBoxFlat CreateAnswerStyle(AnswerVisualState state, bool hoverVariant = false)
    {
        var style = new StyleBoxFlat
        {
            CornerRadiusTopLeft = 28,
            CornerRadiusTopRight = 28,
            CornerRadiusBottomRight = 28,
            CornerRadiusBottomLeft = 28,
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            ContentMarginLeft = 74,
            ContentMarginTop = 14,
            ContentMarginRight = 18,
            ContentMarginBottom = 14
        };

        switch (state)
        {
            case AnswerVisualState.Locked:
                style.BgColor = hoverVariant ? new Color(0.11f, 0.16f, 0.34f, 0.98f) : new Color(0.085f, 0.12f, 0.28f, 0.92f);
                style.BorderWidthBottom = hoverVariant ? 10 : 6;
                style.BorderColor = new Color(0.42f, 0.5f, 0.78f, 0.75f);
                break;
            case AnswerVisualState.Correct:
                style.BgColor = new Color(0.07f, 0.32f, 0.21f, 1f);
                style.BorderWidthBottom = 10;
                style.BorderColor = new Color(0.95f, 0.81f, 0.35f, 1f);
                style.ShadowColor = new Color(0f, 0.3f, 0.14f, 0.45f);
                style.ShadowSize = 16;
                style.ShadowOffset = new Vector2(0, 7);
                break;
            case AnswerVisualState.Wrong:
                style.BgColor = new Color(0.26f, 0.08f, 0.11f, 0.96f);
                style.BorderWidthBottom = 8;
                style.BorderColor = new Color(0.78f, 0.31f, 0.31f, 0.95f);
                break;
            default:
                style.BgColor = hoverVariant ? new Color(0.08f, 0.3f, 0.8f, 1f) : new Color(0.04f, 0.2f, 0.63f, 1f);
                style.BorderWidthBottom = hoverVariant ? 14 : 12;
                style.BorderColor = hoverVariant ? new Color(1f, 0.84f, 0.35f, 1f) : new Color(1f, 0.72f, 0.18f, 1f);
                style.ShadowColor = new Color(0, 0, 0, hoverVariant ? 0.65f : 0.55f);
                style.ShadowSize = hoverVariant ? 20 : 18;
                style.ShadowOffset = new Vector2(0, hoverVariant ? 10 : 8);
                break;
        }

        return style;
    }
}
