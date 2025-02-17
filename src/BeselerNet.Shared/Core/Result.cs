using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BeselerNet.Shared.Core;

[DebuggerDisplay("{Succeeded ? \"Success\" : \"Failure\"}")]
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly Exception? _exception;

    private Result(T value) => _value = value;
    private Result(Exception exception) => _exception = exception;

    public bool HasSucceeded => _exception is null;
    public bool HasFailed => HasSucceeded is false;

    public bool Succeeded([NotNullWhen(true)] out T? value)
    {
        value = _value;
        return HasSucceeded;
    }

    public bool Failed([NotNullWhen(true)] out Exception? exception)
    {
        exception = _exception;
        return HasFailed;
    }

    public void Deconstruct(out T? value, out Exception? exception)
    {
        value = _value;
        exception = _exception;
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Exception, TResult> onFailure) =>
        HasSucceeded ? onSuccess(_value!) : onFailure(_exception!);

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Exception exception) => new(exception);
}

public readonly struct Success(string? message = null)
{
    public static readonly Success Default = new();
    public string? Message { get; } = message;
}
