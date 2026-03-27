using Godot;
using IFeelDumpQuiz;
using IFeelDumpQuiz.Services;
using System.Linq;

public partial class ResultScene : Control
{
    public override void _Ready()
    {
        var summary = GetNode<RichTextLabel>("RootMargin/MainPanel/MainVBox/SummaryPanel/SummaryText");
        summary.Clear();

        TrySaveHistory(summary);

        var ranking = GameSession.Players
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.CorrectAnswers)
            .ThenBy(p => p.Name)
            .ToList();

        var place = 1;
        foreach (var player in ranking)
        {
            var accuracy = GameSession.GetAccuracy(player);
            summary.AppendText($"{place}. {player.Name}\n");
            summary.AppendText($"   {player.Score} Punkte | Richtig: {player.CorrectAnswers} | Falsch: {player.WrongAnswers} | Trefferquote: {accuracy:0.0}%\n\n");
            place++;
        }

        GetNode<Button>("RootMargin/MainPanel/MainVBox/ButtonRow/BtnMainMenu").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        GetNode<Button>("RootMargin/MainPanel/MainVBox/ButtonRow/BtnHistory").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/HistoryScene.tscn");
    }

    private static void TrySaveHistory(RichTextLabel summary)
    {
        if (GameSession.HistorySaved || GameSession.Players.Count == 0 || GameSession.AnswerHistory.Count == 0)
        {
            return;
        }

        try
        {
            GameSession.MarkFinished();
            var repository = StatsRepository.CreateDefault();
            repository.SaveCurrentGame();
            GameSession.MarkHistorySaved();
        }
        catch (System.Exception ex)
        {
            summary.AppendText($"Hinweis: Spielhistorie konnte nicht gespeichert werden ({ex.Message}).\n\n");
        }
    }
}
