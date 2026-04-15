using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

public sealed class SmoothFilter : IFilter
{
    public string Name => "Smooth";

    public BitmapBuffer Apply(BitmapBuffer source, IReadOnlyDictionary<string, double> parameters)
    {
        ArgumentNullException.ThrowIfNull(source);
        var radius = FilterParameterHelper.GetRequiredInt(parameters, "radius", 1, 5);
        return BitmapFilterConvolution.Smooth(source, radius);
    }
}
