using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

public sealed class GammaFilter : IFilter
{
    public string Name => "Gamma";

    public BitmapBuffer Apply(BitmapBuffer source, IReadOnlyDictionary<string, double> parameters)
    {
        ArgumentNullException.ThrowIfNull(source);
        var gamma = FilterParameterHelper.GetRequiredDouble(parameters, "gamma", 0.1d, 5d);
        return BitmapFilterBasic.Gamma(source, gamma);
    }
}
