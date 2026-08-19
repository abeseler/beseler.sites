using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace BeselerNet.Web.Shared;

internal sealed class ClientClock
{
    public string? TimeZone { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public int Day { get; private set; }

    public async Task EnsureAsync(IJSRuntime js)
    {
        if (!string.IsNullOrWhiteSpace(TimeZone))
            return;

        var snap = await js.InvokeAsync<Snapshot>("beselerClock.snapshot");
        TimeZone = string.IsNullOrWhiteSpace(snap.TimeZone) ? "UTC" : snap.TimeZone;
        Year = snap.Year;
        Month = snap.Month;
        Day = snap.Day;
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
