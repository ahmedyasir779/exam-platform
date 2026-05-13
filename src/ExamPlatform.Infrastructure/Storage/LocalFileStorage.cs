using ExamPlatform.Domain.Interfaces;

namespace ExamPlatform.Infrastructure.Storage;

public class LocalFileStorage(string basePath) : IFileStorage
{
    public async Task<string> SaveAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        Directory.CreateDirectory(basePath);
        var safeName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(basePath, safeName);
        await using var fs = File.Create(fullPath);
        await fileStream.CopyToAsync(fs, ct);
        return fullPath;
    }

    public Task<Stream> ReadAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");
        return Task.FromResult<Stream>(File.OpenRead(filePath));
    }

    public Task DeleteAsync(string filePath, CancellationToken ct = default)
    {
        if (File.Exists(filePath)) File.Delete(filePath);
        return Task.CompletedTask;
    }
}
