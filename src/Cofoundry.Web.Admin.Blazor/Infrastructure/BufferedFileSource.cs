namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// An uploaded file held in memory. Browser file streams can only be read once and only while the
/// circuit is connected, whereas the domain may open the file more than once (e.g. to read the image
/// dimensions and then to save it), so uploads are buffered on the server first.
/// </summary>
public sealed class BufferedFileSource : IFileSource
{
    /// <summary>
    /// Matches the default ASP.NET Core request body limit, which capped uploads through the
    /// AngularJS admin's api.
    /// </summary>
    public const long MaxFileSize = 30_000_000;

    private readonly byte[] _data;

    private BufferedFileSource(string fileName, string? mimeType, byte[] data)
    {
        FileName = fileName;
        MimeType = mimeType;
        _data = data;
    }

    public string FileName { get; }

    public string? MimeType { get; }

    public long Length => _data.LongLength;

    public Task<Stream> OpenReadStreamAsync()
    {
        return Task.FromResult<Stream>(new MemoryStream(_data, writable: false));
    }

    public static async Task<BufferedFileSource> ReadAsync(Stream stream, string fileName, string? mimeType, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);

        return new BufferedFileSource(fileName, string.IsNullOrEmpty(mimeType) ? null : mimeType, buffer.ToArray());
    }
}
