using IFeelDumpQuiz.Models;

namespace IFeelDumpQuiz.Services;

public static class ShuffleService
{
    private static readonly Random Random = new();

    public static List<Player> CreateShuffledOrder(IEnumerable<Player> players)
    {
        var list = players.ToList();

        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }
}
