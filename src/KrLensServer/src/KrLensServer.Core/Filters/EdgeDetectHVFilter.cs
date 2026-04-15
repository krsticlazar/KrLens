using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

public sealed class EdgeDetectHVFilter : IFilter
{
    public string Name => "EdgeDetectHV";

    public BitmapBuffer Apply(BitmapBuffer source, IReadOnlyDictionary<string, double> parameters)
    {
        ArgumentNullException.ThrowIfNull(source);
        var direction = FilterParameterHelper.GetRequiredInt(parameters, "direction", 0, 2);
        return BitmapFilterConvolution.EdgeDetectHV(source, direction);
    }
}
