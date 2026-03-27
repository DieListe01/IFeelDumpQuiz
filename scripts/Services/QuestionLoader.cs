using System.Text.Json;
using Godot;
using IFeelDumpQuiz.Models;

namespace IFeelDumpQuiz.Services;

public static class QuestionLoader
{
    public static List<Question> LoadFromJson(string resourcePath)
    {
        if (!Godot.FileAccess.FileExists(resourcePath))
        {
            GD.PushError($"Fragen-Datei nicht gefunden: {resourcePath}");
            return new List<Question>();
        }

        using var file = Godot.FileAccess.Open(resourcePath, Godot.FileAccess.ModeFlags.Read);
        var json = file.GetAsText();
        var items = JsonSerializer.Deserialize<List<Question>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return items ?? new List<Question>();
    }
}
