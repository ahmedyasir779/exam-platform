namespace ExamPlatform.Domain.Interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream fileStream, string fileName, CancellationToken ct = default);
    Task<Stream> ReadAsync(string filePath, CancellationToken ct = default);
    Task DeleteAsync(string filePath, CancellationToken ct = default);
}
