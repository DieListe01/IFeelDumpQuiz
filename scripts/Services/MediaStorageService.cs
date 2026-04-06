using Godot;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IFeelDumpQuiz.Services;

public static class MediaStorageService
{
    private static readonly System.Net.Http.HttpClient HttpClient = new();

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

    public static async Task<QuestionMediaData> DownloadQuestionMediaAsync(string sourceUrl, string mediaType)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Die URL ist ungueltig.");
        }

        using var response = await HttpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync();
        if (bytes.Length == 0)
        {
            throw new InvalidOperationException("Die URL liefert keine Mediendaten.");
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        var fileName = SanitizeFileName(Path.GetFileName(uri.LocalPath));
        if (string.IsNullOrWhiteSpace(fileName))
        {
            var extension = GetExtensionFromMimeType(contentType, mediaType);
            fileName = $"downloaded_{Guid.NewGuid():N}{extension}";
        }

        return new QuestionMediaData
        {
            MediaType = mediaType,
            OriginalFileName = fileName,
            MimeType = string.IsNullOrWhiteSpace(contentType) ? GetMimeType(fileName, mediaType) : contentType,
            BinaryData = bytes,
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

    private static string GetExtensionFromMimeType(string mimeType, string mediaType)
    {
        return (mimeType.ToLowerInvariant(), mediaType) switch
        {
            ("image/png", _) => ".png",
            ("image/jpeg", _) => ".jpg",
            ("image/webp", _) => ".webp",
            ("audio/mpeg", _) => ".mp3",
            ("audio/wav", _) => ".wav",
            ("audio/ogg", _) => ".ogg",
            _ when mediaType == "audio" => ".bin",
            _ => ".bin"
        };
    }
}
