using KrLensServer.Core.Exceptions;

namespace KrLensServer.Core.Filters;

public sealed class FilterRegistry
{
    private readonly IReadOnlyDictionary<string, IFilter> _filters;

    public FilterRegistry(IEnumerable<IFilter> filters)
    {
        ArgumentNullException.ThrowIfNull(filters);
        _filters = filters.ToDictionary(filter => filter.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> Names => _filters.Keys.ToArray();

    public IFilter GetRequired(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_filters.TryGetValue(name, out var filter))
        {
            return filter;
        }

        throw new FilterParameterException($"Filter '{name}' is not registered.");
    }
}
