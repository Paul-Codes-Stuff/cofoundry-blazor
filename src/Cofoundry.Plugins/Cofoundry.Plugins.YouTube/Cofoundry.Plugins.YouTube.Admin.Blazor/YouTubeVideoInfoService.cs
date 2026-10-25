using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cofoundry.Plugins.YouTube.Admin.Blazor;

/// <summary>
/// Looks up videos with the YouTube Data API, the equivalent of the AngularJS shared.youTubeService.
/// That called the API from the browser, so the API key in <see cref="YouTubeSettings"/> had to be sent to
/// it (the youtube-settings admin api); here it's called from the server, so the key stays there.
/// </summary>
public partial class YouTubeVideoInfoService
{
    private const string ServiceUrl = "https://www.googleapis.com/youtube/v3/videos";

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(20) };

    private readonly YouTubeSettings _settings;

    public YouTubeVideoInfoService(YouTubeSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Returns the video's details, or null if YouTube doesn't have it. Without an API key nothing can
    /// be looked up, so only the id is returned, as in the AngularJS admin.
    /// </summary>
    /// <exception cref="HttpRequestException">YouTube could not be reached or returned an error.</exception>
    public async Task<YouTubeVideo?> GetVideoInfoAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return new YouTubeVideo() { Id = id };
        }

        var url = $"{ServiceUrl}?id={Uri.EscapeDataString(id)}&part=snippet&key={Uri.EscapeDataString(_settings.ApiKey)}";
        using var response = await HttpClient.GetAsync(url, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!json.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
        {
            return null;
        }

        var snippet = items[0].GetProperty("snippet");
        var video = new YouTubeVideo()
        {
            Id = id,
            Title = GetString(snippet, "title"),
            Description = GetString(snippet, "description"),
            PublishDate = GetString(snippet, "publishedAt") is string date
                ? DateTimeOffset.Parse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
                : null
        };

        if (snippet.TryGetProperty("thumbnails", out var thumbnails) && thumbnails.TryGetProperty("high", out var thumbnail))
        {
            video.ThumbnailUrl = GetString(thumbnail, "url");
            video.ThumbnailWidth = thumbnail.TryGetProperty("width", out var width) ? width.GetInt32() : null;
            video.ThumbnailHeight = thumbnail.TryGetProperty("height", out var height) ? height.GetInt32() : null;
        }

        return video;
    }

    /// <summary>
    /// Reads the video id from a YouTube url, or returns an 11 character id as it is. Returns null if
    /// neither matches. The same patterns as the AngularJS cms-form-field-youtube-id.
    /// </summary>
    public static string? ParseVideoId(string? urlOrId)
    {
        if (string.IsNullOrWhiteSpace(urlOrId))
        {
            return null;
        }

        urlOrId = urlOrId.Trim();

        if (IdRegex().IsMatch(urlOrId))
        {
            return urlOrId;
        }

        var match = UrlRegex().Match(urlOrId);

        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? GetString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    [GeneratedRegex("""^[^"&?/ ]{11}$""")]
    private static partial Regex IdRegex();

    [GeneratedRegex("""(?:youtube\.com/(?:[^/]+/.+/|(?:v|e(?:mbed)?)/|.*[?&]v=)|youtu\.be/)([^"&?/ ]{11})""", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();
}
