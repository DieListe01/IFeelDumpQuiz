using Godot;
using System.Collections.Generic;

namespace IFeelDumpQuiz;

public static class AppState
{
    private const string SettingsPath = "user://settings.cfg";
    private static bool _loaded;

    public static GameConfig LastGameConfig { get; private set; } = new();
    public static List<string> LastPlayerNames { get; private set; } = new() { "", "", "", "" };

    public static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        var configFile = new ConfigFile();
        if (configFile.Load(SettingsPath) != Error.Ok)
        {
            return;
        }

        LastGameConfig = new GameConfig
        {
            QuestionCount = (int)configFile.GetValue("setup", "question_count", 5),
            Category = configFile.GetValue("setup", "category", "Alle").AsString(),
            TimePerQuestionSeconds = (int)configFile.GetValue("setup", "time_per_question_seconds", 20)
        };

        var playerCount = (int)configFile.GetValue("setup", "player_count", 2);
        LastGameConfig.PlayerNames = new List<string>();
        LastPlayerNames = new List<string>();
        for (var i = 0; i < 4; i++)
        {
            var name = configFile.GetValue("players", $"player_{i}", "").AsString();
            LastPlayerNames.Add(name);
            if (i < playerCount)
            {
                LastGameConfig.PlayerNames.Add(name);
            }
        }
    }

    public static void SaveSetup(GameConfig config, List<string> playerNames)
    {
        EnsureLoaded();

        LastGameConfig = new GameConfig
        {
            PlayerNames = new List<string>(config.PlayerNames),
            QuestionCount = config.QuestionCount,
            Category = config.Category,
            TimePerQuestionSeconds = config.TimePerQuestionSeconds
        };

        LastPlayerNames = new List<string>(playerNames);
        while (LastPlayerNames.Count < 4)
        {
            LastPlayerNames.Add(string.Empty);
        }

        var configFile = new ConfigFile();
        configFile.SetValue("setup", "player_count", config.PlayerNames.Count);
        configFile.SetValue("setup", "question_count", config.QuestionCount);
        configFile.SetValue("setup", "category", config.Category);
        configFile.SetValue("setup", "time_per_question_seconds", config.TimePerQuestionSeconds);

        for (var i = 0; i < 4; i++)
        {
            configFile.SetValue("players", $"player_{i}", LastPlayerNames[i]);
        }

        configFile.Save(SettingsPath);
    }
}
