using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

public sealed class StuckiFilter : IFilter
{
    public string Name => "Stucki";

    public BitmapBuffer Apply(BitmapBuffer source, IReadOnlyDictionary<string, double> parameters)
    {
        ArgumentNullException.ThrowIfNull(source);
        return BitmapFilterTonal.Stucki(source);
    }
}
