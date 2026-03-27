using Godot;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace IFeelDumpQuiz.Services;

public sealed class StatsRepository
{
    private readonly string _databasePath;

    public StatsRepository(string databasePath)
    {
        _databasePath = databasePath;
    }

    public static StatsRepository CreateDefault()
    {
        return new StatsRepository(ProjectSettings.GlobalizePath("user://quiz_history.db"));
    }

    public void Initialize()
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = CreateConnection();
        connection.Open();
        EnableForeignKeys(connection);

        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS games (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    created_at TEXT NOT NULL,
    category TEXT NOT NULL,
    question_count INTEGER NOT NULL,
    time_per_question_seconds INTEGER NOT NULL,
    duration_seconds INTEGER NULL
);

CREATE TABLE IF NOT EXISTS players (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS game_players (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    game_id INTEGER NOT NULL,
    player_name TEXT NOT NULL,
    score INTEGER NOT NULL DEFAULT 0,
    correct_answers INTEGER NOT NULL DEFAULT 0,
    wrong_answers INTEGER NOT NULL DEFAULT 0,
    accuracy REAL NOT NULL DEFAULT 0,
    FOREIGN KEY (game_id) REFERENCES games(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS answers (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    game_id INTEGER NOT NULL,
    question_id INTEGER NOT NULL,
    player_name TEXT NOT NULL,
    selected_index INTEGER NOT NULL,
    is_correct INTEGER NOT NULL,
    answered_at TEXT NOT NULL,
    FOREIGN KEY (game_id) REFERENCES games(id) ON DELETE CASCADE
);";
        command.ExecuteNonQuery();
    }

    public void SaveCurrentGame()
    {
        Initialize();

        using var connection = CreateConnection();
        connection.Open();
        EnableForeignKeys(connection);
        using var transaction = connection.BeginTransaction();

        var finishedAt = IFeelDumpQuiz.GameSession.FinishedAt ?? DateTime.UtcNow;
        var durationSeconds = (int)Math.Max(0, (finishedAt - IFeelDumpQuiz.GameSession.StartedAt).TotalSeconds);

        using var gameCommand = connection.CreateCommand();
        gameCommand.Transaction = transaction;
        gameCommand.CommandText = @"
INSERT INTO games (created_at, category, question_count, time_per_question_seconds, duration_seconds)
VALUES ($created_at, $category, $question_count, $time_per_question_seconds, $duration_seconds);
SELECT last_insert_rowid();";
        gameCommand.Parameters.AddWithValue("$created_at", IFeelDumpQuiz.GameSession.StartedAt.ToString("O", CultureInfo.InvariantCulture));
        gameCommand.Parameters.AddWithValue("$category", IFeelDumpQuiz.GameSession.Config.Category);
        gameCommand.Parameters.AddWithValue("$question_count", IFeelDumpQuiz.GameSession.ActiveQuestions.Count);
        gameCommand.Parameters.AddWithValue("$time_per_question_seconds", IFeelDumpQuiz.GameSession.Config.TimePerQuestionSeconds);
        gameCommand.Parameters.AddWithValue("$duration_seconds", durationSeconds);
        var gameId = (long)(gameCommand.ExecuteScalar() ?? 0L);

        foreach (var player in IFeelDumpQuiz.GameSession.Players)
        {
            using var playerCommand = connection.CreateCommand();
            playerCommand.Transaction = transaction;
            playerCommand.CommandText = "INSERT OR IGNORE INTO players (name) VALUES ($name);";
            playerCommand.Parameters.AddWithValue("$name", player.Name);
            playerCommand.ExecuteNonQuery();

            using var gamePlayerCommand = connection.CreateCommand();
            gamePlayerCommand.Transaction = transaction;
            gamePlayerCommand.CommandText = @"
INSERT INTO game_players (game_id, player_name, score, correct_answers, wrong_answers, accuracy)
VALUES ($game_id, $player_name, $score, $correct_answers, $wrong_answers, $accuracy);";
            gamePlayerCommand.Parameters.AddWithValue("$game_id", gameId);
            gamePlayerCommand.Parameters.AddWithValue("$player_name", player.Name);
            gamePlayerCommand.Parameters.AddWithValue("$score", player.Score);
            gamePlayerCommand.Parameters.AddWithValue("$correct_answers", player.CorrectAnswers);
            gamePlayerCommand.Parameters.AddWithValue("$wrong_answers", player.WrongAnswers);
            gamePlayerCommand.Parameters.AddWithValue("$accuracy", IFeelDumpQuiz.GameSession.GetAccuracy(player));
            gamePlayerCommand.ExecuteNonQuery();
        }

        foreach (var answer in IFeelDumpQuiz.GameSession.AnswerHistory)
        {
            using var answerCommand = connection.CreateCommand();
            answerCommand.Transaction = transaction;
            answerCommand.CommandText = @"
INSERT INTO answers (game_id, question_id, player_name, selected_index, is_correct, answered_at)
VALUES ($game_id, $question_id, $player_name, $selected_index, $is_correct, $answered_at);";
            answerCommand.Parameters.AddWithValue("$game_id", gameId);
            answerCommand.Parameters.AddWithValue("$question_id", answer.QuestionId);
            answerCommand.Parameters.AddWithValue("$player_name", answer.PlayerName);
            answerCommand.Parameters.AddWithValue("$selected_index", answer.SelectedIndex);
            answerCommand.Parameters.AddWithValue("$is_correct", answer.IsCorrect ? 1 : 0);
            answerCommand.Parameters.AddWithValue("$answered_at", answer.AnsweredAt.ToString("O", CultureInfo.InvariantCulture));
            answerCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public List<GameHistoryEntry> GetGameHistory()
    {
        Initialize();

        using var connection = CreateConnection();
        connection.Open();
        EnableForeignKeys(connection);

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    g.id,
    g.created_at,
    g.category,
    g.question_count,
    g.time_per_question_seconds,
    COALESCE(g.duration_seconds, 0),
    gp.player_name,
    COALESCE(gp.score, 0),
    COALESCE(gp.correct_answers, 0),
    COALESCE(gp.wrong_answers, 0),
    COALESCE(gp.accuracy, 0)
FROM games g
LEFT JOIN game_players gp ON gp.game_id = g.id
ORDER BY g.created_at DESC, gp.score DESC, gp.player_name ASC;";

        using var reader = command.ExecuteReader();
        var entries = new List<GameHistoryEntry>();
        var entryMap = new Dictionary<long, GameHistoryEntry>();

        while (reader.Read())
        {
            var gameId = reader.GetInt64(0);
            if (!entryMap.TryGetValue(gameId, out var entry))
            {
                entry = new GameHistoryEntry
                {
                    Id = gameId,
                    CreatedAt = DateTime.Parse(reader.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToLocalTime(),
                    Category = reader.GetString(2),
                    QuestionCount = reader.GetInt32(3),
                    TimePerQuestionSeconds = reader.GetInt32(4),
                    DurationSeconds = reader.GetInt32(5)
                };
                entryMap[gameId] = entry;
                entries.Add(entry);
            }

            if (!reader.IsDBNull(6))
            {
                entry.Players.Add(new GameHistoryPlayerEntry
                {
                    Name = reader.GetString(6),
                    Score = reader.GetInt32(7),
                    CorrectAnswers = reader.GetInt32(8),
                    WrongAnswers = reader.GetInt32(9),
                    Accuracy = reader.GetDouble(10)
                });
            }
        }

        return entries;
    }

    public void DeleteGame(long gameId)
    {
        Initialize();

        using var connection = CreateConnection();
        connection.Open();
        EnableForeignKeys(connection);

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM games WHERE id = $gameId;";
        command.Parameters.AddWithValue("$gameId", gameId);
        command.ExecuteNonQuery();
    }

    public void DeleteAllGames()
    {
        Initialize();

        using var connection = CreateConnection();
        connection.Open();
        EnableForeignKeys(connection);

        using var command = connection.CreateCommand();
        command.CommandText = @"
DELETE FROM games;
DELETE FROM sqlite_sequence WHERE name IN ('games', 'game_players', 'answers');";
        command.ExecuteNonQuery();
    }

    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection($"Data Source={_databasePath}");
    }

    private static void EnableForeignKeys(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();
    }
}
