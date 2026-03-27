using Godot;
using IFeelDumpQuiz.Models;
using IFeelDumpQuiz.Services;

namespace IFeelDumpQuiz;

public partial class GameManager : Node
{
    public IFeelDumpQuiz.Models.GameSession? CurrentSession { get; private set; }
    private readonly Queue<Player> _turnQueue = new();

    public override void _Ready()
    {
        GD.Print("GameManager bereit");
    }

    public void StartNewLocalGame(List<Player> players, GameSettings settings, List<Question> allQuestions)
    {
        var selectedQuestions = allQuestions
            .Where(q => settings.Category == "Alle" || q.Category.Equals(settings.Category, StringComparison.OrdinalIgnoreCase))
            .Take(settings.QuestionCount)
            .ToList();

        CurrentSession = new IFeelDumpQuiz.Models.GameSession
        {
            Players = players,
            Settings = settings,
            Questions = selectedQuestions,
            CurrentQuestionIndex = 0,
            StartedAtUtc = DateTime.UtcNow
        };

        BuildTurnQueue(players);
        GD.Print($"Neues Spiel gestartet. Spieler: {players.Count}, Fragen: {selectedQuestions.Count}");
    }

    public Player? GetCurrentStartingPlayer()
    {
        return _turnQueue.Count > 0 ? _turnQueue.Peek() : null;
    }

    public void RotateForNextQuestion()
    {
        if (CurrentSession is null)
        {
            return;
        }

        CurrentSession.CurrentQuestionIndex++;
        BuildTurnQueue(CurrentSession.Players);
    }

    public void RegisterAnswer(int playerLocalId, int selectedAnswer)
    {
        if (CurrentSession is null || CurrentSession.CurrentQuestionIndex >= CurrentSession.Questions.Count)
        {
            return;
        }

        var question = CurrentSession.Questions[CurrentSession.CurrentQuestionIndex];
        var player = CurrentSession.Players.First(x => x.LocalId == playerLocalId);
        var isCorrect = selectedAnswer == question.CorrectAnswer;

        CurrentSession.Answers.Add(new PlayerAnswer
        {
            PlayerLocalId = playerLocalId,
            QuestionId = question.Id,
            SelectedAnswer = selectedAnswer,
            IsCorrect = isCorrect,
            AnsweredAtUtc = DateTime.UtcNow
        });

        if (isCorrect)
        {
            player.Score += 1;
            player.CorrectAnswers += 1;
        }
        else
        {
            player.WrongAnswers += 1;
        }
    }

    private void BuildTurnQueue(IEnumerable<Player> players)
    {
        _turnQueue.Clear();
        foreach (var player in ShuffleService.CreateShuffledOrder(players))
        {
            _turnQueue.Enqueue(player);
        }
    }
}
