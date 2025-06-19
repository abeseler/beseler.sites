namespace BeselerNet.Shared.Core;

public sealed class PassiveAggressiveException(string message, PassiveAggressionLevel level = PassiveAggressionLevel.None)
    : Exception(level switch
    {
        PassiveAggressionLevel.None => $"{message.TrimEnd('.')}.",
        PassiveAggressionLevel.Some => $"{message.TrimEnd('.')}!",
        PassiveAggressionLevel.Lots => $"{message.TrimEnd('.')}!!1",
        PassiveAggressionLevel.Toxic => $"{message.ToUpperInvariant().TrimEnd('.')}!",
        PassiveAggressionLevel.Explosive => $"{message.ToUpperInvariant().TrimEnd('.')} - WTF IS WRONG WITH YOU?!",
        _ => message
    });

public enum PassiveAggressionLevel
{
    None,
    Some,
    Lots,
    Toxic,
    Explosive
}
