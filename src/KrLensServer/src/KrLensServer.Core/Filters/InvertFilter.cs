using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

public sealed class InvertFilter : IFilter
{
    public string Name => "Invert";

    public BitmapBuffer Apply(BitmapBuffer source, IReadOnlyDictionary<string, double> parameters)
    {
        ArgumentNullException.ThrowIfNull(source);
        return BitmapFilterBasic.Invert(source);
    }
}
