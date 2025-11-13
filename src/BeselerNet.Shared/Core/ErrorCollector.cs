namespace BeselerNet.Shared.Core;

public struct ErrorCollector(IEqualityComparer<string>? comparer = null)
{
    private readonly IEqualityComparer<string>? _comparer = comparer;
    public Dictionary<string, string[]>? Collection { get; private set; }
    public readonly int Count => Collection?.Count ?? 0;
    public ErrorCollector Add(string? key, string? error)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(error))
        {
            return this;
        }
        Collection ??= new(_comparer);
        if (Collection.TryGetValue(key, out var messages))
        {
            Array.Resize(ref messages, messages.Length + 1);
            messages[^1] = error;
        }
        else
        {
            Collection[key] = [error];
        }
        return this;
    }
    public ErrorCollector Add(string prefix, string property, string error) => Add($"{prefix}.{property}", error);
    public ErrorCollector Add(string property, int index, string error) => Add($"{property}[{index}]", error);
    public ErrorCollector Add(string prefix, int index, string property, string error) => Add($"{prefix}[{index}].{property}", error);
    public ErrorCollector Validate<T>(T? item, string propertyName, Action<T, ErrorCollector> validateAction) where T : class
    {
        if (item is null)
        {
            Add(propertyName, "Required.");
        }
        else
        {
            validateAction(item, this);
        }
        return this;
    }
    public readonly ErrorCollector ValidateOptional<T>(T? item, Action<T, ErrorCollector> validateAction) where T : class
    {
        if (item is not null)
        {
            validateAction(item, this);
        }
        return this;
    }
    public ErrorCollector ValidateEach<T>(ReadOnlySpan<T?> items, string propertyName, Action<T, int, ErrorCollector> validateAction) where T : class
    {
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item is null)
            {
                Add(propertyName, i, "Required.");
                continue;
            }
            else
            {
                validateAction(item, i, this);
            }
        }
        return this;
    }
    public readonly ErrorCollector ValidateEachOptional<T>(ReadOnlySpan<T?> items, Action<T, int, ErrorCollector> validateAction) where T : class
    {
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item is not null)
            {
                validateAction(item, i, this);
            }
        }
        return this;
    }
}
