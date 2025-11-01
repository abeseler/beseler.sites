namespace BeselerNet.Web.Services;

internal sealed class FeatureManager
{
    public ValueTask<bool> IsEnabled(string featureName, bool defaultValue = false)
    {
        return ValueTask.FromResult(defaultValue);
    }
}
