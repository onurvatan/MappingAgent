using System.Security.Cryptography;

namespace MappingAgent.Api.Services;

public interface IHashingService
{
    Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken);
}

public sealed class HashingService : IHashingService
{
    public async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(hash);
    }
}
