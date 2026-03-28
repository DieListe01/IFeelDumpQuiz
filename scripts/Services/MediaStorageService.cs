using Godot;
using System;
using System.IO;

namespace IFeelDumpQuiz.Services;

public static class MediaStorageService
{
    public static string ImportQuestionMedia(int questionId, string sourcePath, string preferredFileName)
    {
        var questionDir = ProjectSettings.GlobalizePath($"user://media/questions/{questionId}");
        Directory.CreateDirectory(questionDir);

        var extension = Path.GetExtension(sourcePath);
        var baseName = SanitizeFileName(preferredFileName);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = Guid.NewGuid().ToString("N");
        }

        var fileName = baseName + extension;
        var destinationAbsolutePath = Path.Combine(questionDir, fileName);
        File.Copy(sourcePath, destinationAbsolutePath, true);
        return $"user://media/questions/{questionId}/{fileName}";
    }

    public static void DeleteStoredMedia(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return;
        }

        var absolute = ProjectSettings.GlobalizePath(storedPath);
        if (File.Exists(absolute))
        {
            File.Delete(absolute);
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidChar, '_');
        }

        return name.Trim();
    }
}
