using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

public sealed class HistogramEqualizingFilter : IFilter
{
    public string Name => "HistogramEqualizing";

    public BitmapBuffer Apply(BitmapBuffer source, IReadOnlyDictionary<string, double> parameters)
    {
        ArgumentNullException.ThrowIfNull(source);
        return BitmapFilterTonal.HistogramEqualizing(source);
    }
}
