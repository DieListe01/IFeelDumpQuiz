using IFeelDumpQuiz.Services;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace IFeelDumpQuiz.Repositories;

public sealed class GameRepository
{
    public void EnsureSchema()
    {
        DatabaseMigrationService.EnsureInitialized();

        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS games (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    created_at TEXT NOT NULL,
    category TEXT NOT NULL,
    mode TEXT NOT NULL DEFAULT 'Simultan',
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
    avg_answer_time_ms INTEGER NOT NULL DEFAULT 0,
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
    answer_duration_ms INTEGER NOT NULL DEFAULT 0,
    answered_at TEXT NOT NULL,
    FOREIGN KEY (game_id) REFERENCES games(id) ON DELETE CASCADE
);";
        command.ExecuteNonQuery();
    }

    public void SaveCurrentGame()
    {
        EnsureSchema();

        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();

        var finishedAt = GameSession.FinishedAt ?? DateTime.UtcNow;
        var durationSeconds = (int)Math.Max(0, (finishedAt - GameSession.StartedAt).TotalSeconds);

        using var gameCommand = connection.CreateCommand();
        gameCommand.Transaction = transaction;
        gameCommand.CommandText = @"
INSERT INTO games (created_at, category, mode, question_count, time_per_question_seconds, duration_seconds)
VALUES ($created_at, $category, $mode, $question_count, $time_per_question_seconds, $duration_seconds);
SELECT last_insert_rowid();";
        gameCommand.Parameters.AddWithValue("$created_at", GameSession.StartedAt.ToString("O", CultureInfo.InvariantCulture));
        gameCommand.Parameters.AddWithValue("$category", GameSession.Config.Category);
        gameCommand.Parameters.AddWithValue("$mode", GameSession.Config.AnswerMode);
        gameCommand.Parameters.AddWithValue("$question_count", GameSession.ActiveQuestions.Count);
        gameCommand.Parameters.AddWithValue("$time_per_question_seconds", GameSession.Config.TimePerQuestionSeconds);
        gameCommand.Parameters.AddWithValue("$duration_seconds", durationSeconds);
        var gameId = (long)(gameCommand.ExecuteScalar() ?? 0L);

        foreach (var player in GameSession.Players)
        {
            using var playerCommand = connection.CreateCommand();
            playerCommand.Transaction = transaction;
            playerCommand.CommandText = "INSERT OR IGNORE INTO players (name) VALUES ($name);";
            playerCommand.Parameters.AddWithValue("$name", player.Name);
            playerCommand.ExecuteNonQuery();

            using var gamePlayerCommand = connection.CreateCommand();
            gamePlayerCommand.Transaction = transaction;
            gamePlayerCommand.CommandText = @"
INSERT INTO game_players (game_id, player_name, score, correct_answers, wrong_answers, avg_answer_time_ms, accuracy)
VALUES ($game_id, $player_name, $score, $correct_answers, $wrong_answers, $avg_answer_time_ms, $accuracy);";
            gamePlayerCommand.Parameters.AddWithValue("$game_id", gameId);
            gamePlayerCommand.Parameters.AddWithValue("$player_name", player.Name);
            gamePlayerCommand.Parameters.AddWithValue("$score", player.Score);
            gamePlayerCommand.Parameters.AddWithValue("$correct_answers", player.CorrectAnswers);
            gamePlayerCommand.Parameters.AddWithValue("$wrong_answers", player.WrongAnswers);
            gamePlayerCommand.Parameters.AddWithValue("$avg_answer_time_ms", (long)Math.Round(GameSession.GetAverageAnswerTimeSeconds(player) * 1000.0));
            gamePlayerCommand.Parameters.AddWithValue("$accuracy", GameSession.GetAccuracy(player));
            gamePlayerCommand.ExecuteNonQuery();
        }

        foreach (var answer in GameSession.AnswerHistory)
        {
            using var answerCommand = connection.CreateCommand();
            answerCommand.Transaction = transaction;
            answerCommand.CommandText = @"
INSERT INTO answers (game_id, question_id, player_name, selected_index, is_correct, answer_duration_ms, answered_at)
VALUES ($game_id, $question_id, $player_name, $selected_index, $is_correct, $answer_duration_ms, $answered_at);";
            answerCommand.Parameters.AddWithValue("$game_id", gameId);
            answerCommand.Parameters.AddWithValue("$question_id", answer.QuestionId);
            answerCommand.Parameters.AddWithValue("$player_name", answer.PlayerName);
            answerCommand.Parameters.AddWithValue("$selected_index", answer.SelectedIndex);
            answerCommand.Parameters.AddWithValue("$is_correct", answer.IsCorrect ? 1 : 0);
            answerCommand.Parameters.AddWithValue("$answer_duration_ms", answer.AnswerDurationMs);
            answerCommand.Parameters.AddWithValue("$answered_at", answer.AnsweredAt.ToString("O", CultureInfo.InvariantCulture));
            answerCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public List<GameHistoryEntry> GetGameHistory()
    {
        EnsureSchema();

        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    g.id,
    g.created_at,
    g.category,
    COALESCE(g.mode, 'Simultan'),
    g.question_count,
    g.time_per_question_seconds,
    COALESCE(g.duration_seconds, 0),
    gp.player_name,
    COALESCE(gp.score, 0),
    COALESCE(gp.correct_answers, 0),
    COALESCE(gp.wrong_answers, 0),
    COALESCE(gp.avg_answer_time_ms, 0),
    COALESCE(gp.accuracy, 0)
FROM games g
LEFT JOIN game_players gp ON gp.game_id = g.id
ORDER BY g.created_at DESC, gp.score DESC, gp.player_name ASC;";

        using var reader = command.ExecuteReader();
        var entries = new List<GameHistoryEntry>();
        var byId = new Dictionary<long, GameHistoryEntry>();
        while (reader.Read())
        {
            var id = reader.GetInt64(0);
            if (!byId.TryGetValue(id, out var entry))
            {
                entry = new GameHistoryEntry
                {
                    Id = id,
                    CreatedAt = DateTime.Parse(reader.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToLocalTime(),
                    Category = reader.GetString(2),
                    Mode = reader.GetString(3),
                    QuestionCount = reader.GetInt32(4),
                    TimePerQuestionSeconds = reader.GetInt32(5),
                    DurationSeconds = reader.GetInt32(6)
                };
                byId[id] = entry;
                entries.Add(entry);
            }

            if (!reader.IsDBNull(7))
            {
                entry.Players.Add(new GameHistoryPlayerEntry
                {
                    Name = reader.GetString(7),
                    Score = reader.GetInt32(8),
                    CorrectAnswers = reader.GetInt32(9),
                    WrongAnswers = reader.GetInt32(10),
                    AvgAnswerTimeSeconds = reader.GetInt64(11) / 1000.0,
                    Accuracy = reader.GetDouble(12)
                });
            }
        }

        return entries;
    }

    public void DeleteGame(long gameId)
    {
        EnsureSchema();
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM games WHERE id = $gameId;";
        command.Parameters.AddWithValue("$gameId", gameId);
        command.ExecuteNonQuery();
    }

    public void DeleteAllGames()
    {
        EnsureSchema();
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
DELETE FROM games;
DELETE FROM sqlite_sequence WHERE name IN ('games', 'game_players', 'answers');";
        command.ExecuteNonQuery();
    }
}
