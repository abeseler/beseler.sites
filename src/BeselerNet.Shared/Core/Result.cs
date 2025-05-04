using System.Diagnostics.CodeAnalysis;

namespace BeselerNet.Shared.Core;

public readonly struct Result
{
    private static readonly Result success = new();
    private readonly Exception? _exception;
    public static Result Success => success;
    private Result(Exception exception) => _exception = exception;
    public bool Succeeded() => _exception is null;
    public bool Failed([NotNullWhen(true)] out Exception? exception)
    {
        exception = _exception;
        return exception is not null;
    }
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Exception, TResult> onFailure) =>
        _exception is null ? onSuccess() : onFailure(_exception!);
    public TResult Match<TResult, TState>(TState state, Func<TState, TResult> onSuccess, Func<TState, Exception, TResult> onFailure) =>
        _exception is null ? onSuccess(state) : onFailure(state, _exception!);

    public static implicit operator Result(Exception exception) => new(exception);
}

public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly Exception? _exception;
    private Result(T value) => _value = value;
    private Result(Exception exception) => _exception = exception;
    public bool Succeeded([NotNullWhen(true)] out T? value)
    {
        value = _value;
        return _exception is null;
    }
    public bool Failed([NotNullWhen(true)] out Exception? exception)
    {
        exception = _exception;
        return _exception is not null;
    }
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Exception, TResult> onFailure) =>
        _exception is null ? onSuccess(_value!) : onFailure(_exception!);
    public TResult Match<TResult, TState>(TState state, Func<T, TState, TResult> onSuccess, Func<Exception, TState, TResult> onFailure) =>
        _exception is null ? onSuccess(_value!, state) : onFailure(_exception!, state);

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Exception exception) => new(exception);
}
