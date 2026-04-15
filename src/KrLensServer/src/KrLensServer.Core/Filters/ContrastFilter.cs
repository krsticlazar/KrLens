using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

public sealed class ContrastFilter : IFilter
{
    public string Name => "Contrast";

    public BitmapBuffer Apply(BitmapBuffer source, IReadOnlyDictionary<string, double> parameters)
    {
        ArgumentNullException.ThrowIfNull(source);
        var factor = FilterParameterHelper.GetRequiredDouble(parameters, "factor", 0d, 3d);
        return BitmapFilterBasic.Contrast(source, factor);
    }
}
