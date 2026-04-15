using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

public sealed class GrayscaleFilter : IFilter
{
    public string Name => "Grayscale";

    public BitmapBuffer Apply(BitmapBuffer source, IReadOnlyDictionary<string, double> parameters)
    {
        ArgumentNullException.ThrowIfNull(source);
        return BitmapFilterBasic.GrayScale(source);
    }
}
