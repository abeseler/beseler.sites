using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace BeselerNet.Web.Shared;

internal sealed class ClientClock
{
    public string? TimeZone { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public int Day { get; private set; }

    private DateTimeOffset _validUntil;

    /// <returns>True when the local calendar day changed since the last snapshot.</returns>
    public async Task<bool> EnsureAsync(IJSRuntime js)
    {
        var now = DateTimeOffset.UtcNow;
        var had = !string.IsNullOrWhiteSpace(TimeZone);
        if (had && now < _validUntil)
            return false;

        var previous = (Year, Month, Day);
        var snap = await js.InvokeAsync<Snapshot>("beselerClock.snapshot");
        TimeZone = string.IsNullOrWhiteSpace(snap.TimeZone) ? "UTC" : snap.TimeZone;
        Year = snap.Year;
        Month = snap.Month;
        Day = snap.Day;
        SetValidUntil();
        return had && previous != (Year, Month, Day);
    }

    private void SetValidUntil()
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZone ?? "UTC");
            var nextLocal = new DateTime(Year, Month, Day, 0, 0, 0, DateTimeKind.Unspecified).AddDays(1);
            _validUntil = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(nextLocal, zone), TimeSpan.Zero);
        }
        catch (Exception)
        {
            _validUntil = DateTimeOffset.UtcNow.AddMinutes(30);
        }
    }

    private sealed class Snapshot
    {
        [JsonPropertyName("timeZone")]
        public string TimeZone { get; set; } = "UTC";
        [JsonPropertyName("year")]
        public int Year { get; set; }
        [JsonPropertyName("month")]
        public int Month { get; set; }
        [JsonPropertyName("day")]
        public int Day { get; set; }
    }
}
