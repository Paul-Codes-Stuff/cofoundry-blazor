using Microsoft.JSInterop;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// The signed-in user's browser time zone. Blazor Server components run on the server, so fields that
/// work in the browser's local time ([DateLocal], [DateAndTimeLocal]) need it to convert to and from the
/// UTC values the domain stores, which the AngularJS admin left to the browser. Scoped, so it is read
/// once per circuit.
/// </summary>
public class BrowserTimeZone
{
    private readonly IJSRuntime _jsRuntime;
    private TimeZoneInfo? _timeZone;

    public BrowserTimeZone(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// The browser's time zone, or UTC if it can't be read or isn't known to the server.
    /// </summary>
    public async Task<TimeZoneInfo> GetAsync()
    {
        if (_timeZone != null)
        {
            return _timeZone;
        }

        try
        {
            await using var module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", AdminAssetPaths.TimeZoneScript);
            var id = await module.InvokeAsync<string>("getTimeZone");
            _timeZone = TimeZoneInfo.TryFindSystemTimeZoneById(id, out var timeZone) ? timeZone : TimeZoneInfo.Utc;
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or TaskCanceledException)
        {
            return TimeZoneInfo.Utc;
        }

        return _timeZone;
    }

    /// <summary>
    /// Converts a UTC value to the browser's local time, for display or editing.
    /// </summary>
    public static DateTime? ToLocal(DateTime? utc, TimeZoneInfo timeZone)
    {
        return utc.HasValue
            ? DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc), timeZone), DateTimeKind.Unspecified)
            : null;
    }

    /// <summary>
    /// Converts a value entered in the browser's local time to UTC. A time skipped by a daylight saving
    /// change (which the browser can still accept) uses the zone's standard offset.
    /// </summary>
    public static DateTime? ToUtc(DateTime? local, TimeZoneInfo timeZone)
    {
        if (!local.HasValue)
        {
            return null;
        }

        var value = DateTime.SpecifyKind(local.Value, DateTimeKind.Unspecified);

        return timeZone.IsInvalidTime(value)
            ? DateTime.SpecifyKind(value - timeZone.BaseUtcOffset, DateTimeKind.Utc)
            : TimeZoneInfo.ConvertTimeToUtc(value, timeZone);
    }
}
