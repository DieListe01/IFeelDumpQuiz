using Godot;
using System;
using System.IO;

namespace IFeelDumpQuiz.Services;

public static class MediaStorageService
{
    public static QuestionMediaData ImportQuestionMedia(string sourcePath, string mediaType)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Mediendatei nicht gefunden.", sourcePath);
        }

        var originalFileName = SanitizeFileName(Path.GetFileName(sourcePath));
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            originalFileName = Guid.NewGuid().ToString("N");
        }

        return new QuestionMediaData
        {
            MediaType = mediaType,
            OriginalFileName = originalFileName,
            MimeType = GetMimeType(sourcePath, mediaType),
            BinaryData = File.ReadAllBytes(sourcePath),
            StoredPath = string.Empty
        };
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidChar, '_');
        }

        return name.Trim();
    }

    private static string GetMimeType(string sourcePath, string mediaType)
    {
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        return (mediaType, extension) switch
        {
            ("image", ".png") => "image/png",
            ("image", ".jpg") => "image/jpeg",
            ("image", ".jpeg") => "image/jpeg",
            ("image", ".webp") => "image/webp",
            ("audio", ".mp3") => "audio/mpeg",
            ("audio", ".wav") => "audio/wav",
            ("audio", ".ogg") => "audio/ogg",
            ("audio", _) => "audio/*",
            _ => "image/*"
        };
    }
}
