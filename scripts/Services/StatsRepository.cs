using Microsoft.Data.Sqlite;
using IFeelDumpQuiz.Repositories;
using System;

namespace IFeelDumpQuiz.Services;

public sealed class StatsRepository
{
    private readonly GameRepository _gameRepository = new();

    public static StatsRepository CreateDefault()
    {
        return new StatsRepository();
    }

    public void Initialize()
    {
        _gameRepository.EnsureSchema();
    }

    public void SaveCurrentGame()
    {
        _gameRepository.SaveCurrentGame();
    }

    public List<GameHistoryEntry> GetGameHistory()
    {
        return _gameRepository.GetGameHistory();
    }

    public void DeleteGame(long gameId)
    {
        _gameRepository.DeleteGame(gameId);
    }

    public void DeleteAllGames()
    {
        _gameRepository.DeleteAllGames();
    }
}
