using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

public sealed class FlipFilter : IFilter
{
    public string Name => "Flip";

    public BitmapBuffer Apply(BitmapBuffer source, IReadOnlyDictionary<string, double> parameters)
    {
        ArgumentNullException.ThrowIfNull(source);
        var axis = FilterParameterHelper.GetRequiredInt(parameters, "axis", 0, 1);
        return BitmapFilterDisplacement.Flip(source, axis == 0, axis == 1);
    }
}
