using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

public sealed class FilterPipeline
{
    private readonly FilterRegistry _filterRegistry;

    public FilterPipeline(FilterRegistry filterRegistry)
    {
        _filterRegistry = filterRegistry;
    }

    public BitmapBuffer Apply(BitmapBuffer source, IEnumerable<FilterRequest> filters)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(filters);

        var current = source;
        foreach (var request in filters)
        {
            ArgumentNullException.ThrowIfNull(request);
            var filter = _filterRegistry.GetRequired(request.Filter);
            current = filter.Apply(current, request.Parameters);
        }

        return current;
    }
}
