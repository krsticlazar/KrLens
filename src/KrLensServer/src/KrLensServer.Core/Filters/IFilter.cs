using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

public interface IFilter
{
    string Name { get; }

    BitmapBuffer Apply(BitmapBuffer source, IReadOnlyDictionary<string, double> parameters);
}
