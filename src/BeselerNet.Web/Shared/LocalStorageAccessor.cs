using System.Text.Json;
using Microsoft.JSInterop;

namespace BeselerNet.Web.Shared;

internal sealed class LocalStorageAccessor(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public async ValueTask SetItemAsync<T>(string key, T value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, JsonSerializer.Serialize(value, JsonSerializerOptions.Web));
        }
        catch
        {
        }
    }

    public async ValueTask<T?> GetItemAsync<T>(string key)
    {
        var read = await TryGetItemAsync<T>(key);
        return read.Value;
    }

    public async ValueTask<StorageRead<T>> TryGetItemAsync<T>(string key)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
            return json is null
                ? new(true, default)
                : new(true, JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web));
        }
        catch (InvalidOperationException)
        {
            return new(false, default);
        }
        catch (JSDisconnectedException)
        {
            return new(false, default);
        }
        catch (JSException)
        {
            return new(false, default);
        }
    }

    public async ValueTask RemoveItemAsync(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch
        {
        }
    }
}

internal readonly record struct StorageRead<T>(bool Available, T? Value);
