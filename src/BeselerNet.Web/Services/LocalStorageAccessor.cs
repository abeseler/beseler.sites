using Microsoft.JSInterop;
using System.Text.Json;

namespace BeselerNet.Web.Services;
internal sealed class LocalStorageAccessor(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public async ValueTask SetItemAsync<T>(string key, T value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, JsonSerializer.Serialize(value, JsonSerializerOptions.Web));
        }
        catch {}
    }

    public async ValueTask<T?> GetItemAsync<T>(string key)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
            if (json is null)
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web);
        }
        catch
        {
            return default;
        }
    }

    public async ValueTask RemoveItemAsync(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch {}
    }
}
