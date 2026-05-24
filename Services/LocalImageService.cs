using Microsoft.AspNetCore.Components.Forms;

namespace GameLogBook.Services;

public class LocalImageService
{
    private const long MaxImageBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp"
        };

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp"
        };

    private readonly HttpClient httpClient;

    public LocalImageService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<string> SaveUploadedImageAsync(
        IBrowserFile file,
        string category,
        CancellationToken cancellationToken = default)
    {
        ValidateContentType(file.ContentType);

        string extension = GetExtension(file.Name, file.ContentType);
        string relativePath = CreateRelativePath(category, extension);
        string absolutePath = GetAbsolutePath(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using Stream inputStream = file.OpenReadStream(MaxImageBytes, cancellationToken);
        await using FileStream outputStream = File.Create(absolutePath);
        await inputStream.CopyToAsync(outputStream, cancellationToken);

        return relativePath;
    }

    public Task<string> SaveUploadedTempImageAsync(
        IBrowserFile file,
        CancellationToken cancellationToken = default)
    {
        return SaveUploadedImageAsync(file, "temp", cancellationToken);
    }

    public async Task<string> DownloadImageAsync(
        string imageUrl,
        string category,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(imageUrl.Trim(), UriKind.Absolute, out Uri? uri)
            || uri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("Enter a valid HTTP or HTTPS image URL.");
        }

        using HttpResponseMessage response = await httpClient.GetAsync(uri,
                                                                       HttpCompletionOption.ResponseHeadersRead,
                                                                       cancellationToken);
        response.EnsureSuccessStatusCode();

        string contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        ValidateContentType(contentType);

        long? contentLength = response.Content.Headers.ContentLength;
        if (contentLength > MaxImageBytes)
        {
            throw new InvalidOperationException("Image is larger than the 10 MB limit.");
        }

        string extension = GetExtension(uri.AbsolutePath, contentType);
        string relativePath = CreateRelativePath(category, extension);
        string absolutePath = GetAbsolutePath(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using Stream inputStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using FileStream outputStream = File.Create(absolutePath);
        await CopyToFileWithLimitAsync(inputStream, outputStream, cancellationToken);

        return relativePath;
    }

    public Task<string> DownloadTempImageAsync(
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        return DownloadImageAsync(imageUrl, "temp", cancellationToken);
    }

    public Task<string> MoveTempImageAsync(
        string tempImagePath,
        string category,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tempImagePath) || !IsTempImagePath(tempImagePath))
        {
            throw new InvalidOperationException("No temporary image is ready to save.");
        }

        string sourcePath = GetAbsolutePath(tempImagePath);
        if (!File.Exists(sourcePath))
        {
            throw new InvalidOperationException("The temporary image could not be found.");
        }

        string extension = Path.GetExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            extension = ".img";
        }

        string relativePath = CreateRelativePath(category, extension);
        string destinationPath = GetAbsolutePath(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Move(sourcePath, destinationPath);

        return Task.FromResult(relativePath);
    }

    public Task DeleteTempImageAsync(string? tempImagePath)
    {
        if (string.IsNullOrWhiteSpace(tempImagePath) || !IsTempImagePath(tempImagePath))
        {
            return Task.CompletedTask;
        }

        string absolutePath = GetAbsolutePath(tempImagePath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    public async Task<string?> GetImageSourceAsync(string? imagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || Uri.TryCreate(imagePath, UriKind.Absolute, out _))
        {
            return null;
        }

        string absolutePath = GetAbsolutePath(imagePath);
        if (!File.Exists(absolutePath))
        {
            return null;
        }

        byte[] imageBytes = await File.ReadAllBytesAsync(absolutePath, cancellationToken);
        string contentType = GetContentType(Path.GetExtension(absolutePath));

        return $"data:{contentType};base64,{Convert.ToBase64String(imageBytes)}";
    }

    public async Task<string> GetUploadPreviewSourceAsync(
        IBrowserFile file,
        CancellationToken cancellationToken = default)
    {
        ValidateContentType(file.ContentType);

        await using Stream stream = file.OpenReadStream(MaxImageBytes, cancellationToken);
        using MemoryStream memoryStream = new();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        return $"data:{file.ContentType};base64,{Convert.ToBase64String(memoryStream.ToArray())}";
    }

    private static async Task CopyToFileWithLimitAsync(
        Stream inputStream,
        FileStream outputStream,
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[81920];
        long totalBytes = 0;

        while (true)
        {
            int bytesRead = await inputStream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            totalBytes += bytesRead;
            if (totalBytes > MaxImageBytes)
            {
                throw new InvalidOperationException("Image is larger than the 10 MB limit.");
            }

            await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }
    }

    private static void ValidateContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)
            || !AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Use a JPG, PNG, GIF, or WebP image.");
        }
    }

    private static string CreateRelativePath(string category, string extension)
    {
        string safeCategory = string.Join("_",
                                          category.Split(Path.GetInvalidFileNameChars(),
                                                         StringSplitOptions.RemoveEmptyEntries));

        return Path.Combine("images", safeCategory, $"{Guid.NewGuid():N}{extension}");
    }

    private static bool IsTempImagePath(string imagePath)
    {
        string normalizedPath = imagePath.Replace('\\', '/');

        return normalizedPath.StartsWith("images/temp/", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetAbsolutePath(string relativePath)
    {
        string normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar)
                                            .Replace('\\', Path.DirectorySeparatorChar);

        return Path.Combine(FileSystem.AppDataDirectory, normalizedPath);
    }

    private static string GetExtension(string pathOrName, string contentType)
    {
        string extension = Path.GetExtension(pathOrName);
        if (!string.IsNullOrWhiteSpace(extension) && AllowedExtensions.Contains(extension))
        {
            return extension.ToLowerInvariant();
        }

        return contentType switch
               {
                   "image/jpeg" => ".jpg",
                   "image/png" => ".png",
                   "image/gif" => ".gif",
                   "image/webp" => ".webp",
                   _ => ".img"
               };
    }

    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
               {
                   ".jpg" or ".jpeg" => "image/jpeg",
                   ".png" => "image/png",
                   ".gif" => "image/gif",
                   ".webp" => "image/webp",
                   _ => "application/octet-stream"
               };
    }
}
