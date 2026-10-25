using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cofoundry.Plugins.Vimeo.Admin.Blazor;

/// <summary>
/// Looks up videos with Vimeo's oEmbed api, the equivalent of the AngularJS shared.vimeoService, which
/// called it from the browser. Here it's called from the server; the admin's url is sent as the referrer, as
/// the browser did, so videos that can only be embedded on the site's domain are still found.
/// </summary>
public partial class VimeoVideoInfoService
{
    private const string ServiceUrl = "https://vimeo.com/api/oembed.json?url=https%3A%2F%2Fvimeo.com%2F";

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(20) };

    /// <summary>
    /// Returns the video's details.
    /// </summary>
    /// <param name="id">The numeric video id.</param>
    /// <param name="referrer">The admin's url, for videos restricted to the site's domain.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="VimeoVideoLookupException">
    /// Vimeo didn't return the video; the message says why, worded as in the AngularJS admin.
    /// </exception>
    public async Task<VimeoVideo> GetVideoInfoAsync(long id, string? referrer, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ServiceUrl + id.ToString(CultureInfo.InvariantCulture));
        if (Uri.TryCreate(referrer, UriKind.Absolute, out var referrerUri))
        {
            request.Headers.Referrer = referrerUri;
        }

        try
        {
            using var response = await HttpClient.SendAsync(request, cancellationToken);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    break;
                case HttpStatusCode.NotFound:
                    throw new VimeoVideoLookupException("You aren't able to access the video because of privacy or permissions issues, or because the video is still transcoding.");
                case HttpStatusCode.Forbidden:
                    throw new VimeoVideoLookupException("Embed permissions are disabled for this video, so you can't embed it.");
                default:
                    throw new VimeoVideoLookupException(UnexpectedErrorMessage);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return Map(json.RootElement, id);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException or FormatException)
        {
            throw new VimeoVideoLookupException(UnexpectedErrorMessage, ex);
        }
    }

    /// <summary>
    /// Reads the video id from a Vimeo url (including channel and group urls), or returns a numeric id as it
    /// is. Returns null if neither matches. The same patterns as the AngularJS cms-form-field-vimeo-id.
    /// </summary>
    public static long? ParseVideoId(string? urlOrId)
    {
        if (string.IsNullOrWhiteSpace(urlOrId))
        {
            return null;
        }

        urlOrId = urlOrId.Trim();
        var id = IdRegex().IsMatch(urlOrId) ? urlOrId : UrlRegex().Match(urlOrId) is { Success: true } match ? match.Groups[5].Value : null;

        return long.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var videoId) ? videoId : null;
    }

    private const string UnexpectedErrorMessage = "Something unexpected happened whilst connecting to the Vimeo API.";

    private static VimeoVideo Map(JsonElement data, long id)
    {
        return new VimeoVideo()
        {
            Id = data.TryGetProperty("video_id", out var videoId) && videoId.ValueKind == JsonValueKind.Number ? videoId.GetInt64() : id,
            Title = GetString(data, "title"),
            // As the AngularJS dialog did: oEmbed descriptions may contain html, the field is plain text.
            Description = GetString(data, "description") is string description ? TagRegex().Replace(description, string.Empty) : null,
            Width = GetInt(data, "width"),
            Height = GetInt(data, "height"),
            Duration = GetInt(data, "duration"),
            UploadDate = GetString(data, "upload_date") is string date
                ? DateTime.Parse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
                : null,
            ThumbnailUrl = GetString(data, "thumbnail_url"),
            ThumbnailWidth = GetInt(data, "thumbnail_width"),
            ThumbnailHeight = GetInt(data, "thumbnail_height")
        };
    }

    private static string? GetString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static int? GetInt(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : null;
    }

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex IdRegex();

    // The AngularJS pattern, including its [A-z] (which also matches a few punctuation characters).
    [GeneratedRegex(@"^.*(vimeo\.com/)((channels/[A-z]+/)|(groups/[A-z]+/videos/))?([0-9]+)")]
    private static partial Regex UrlRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();
}

/// <summary>
/// Vimeo didn't return a video. The message is shown to the user.
/// </summary>
public class VimeoVideoLookupException : Exception
{
    public VimeoVideoLookupException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
