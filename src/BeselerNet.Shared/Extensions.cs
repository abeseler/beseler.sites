using System.Text.RegularExpressions;

namespace BeselerNet.Shared;

internal partial class Extensions
{

    [GeneratedRegex("^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")]
    public static partial Regex BasicEmailRegex();
}
