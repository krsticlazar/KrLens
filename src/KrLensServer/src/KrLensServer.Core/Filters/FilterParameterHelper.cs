using KrLensServer.Core.Exceptions;

namespace KrLensServer.Core.Filters;

internal static class FilterParameterHelper
{
    public static double GetRequiredDouble(
        IReadOnlyDictionary<string, double> parameters,
        string name,
        double min,
        double max)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (!parameters.TryGetValue(name, out var value))
        {
            throw new FilterParameterException($"Missing required parameter '{name}'.");
        }

        if (value < min || value > max)
        {
            throw new FilterParameterException($"Parameter '{name}' must be between {min} and {max}.");
        }

        return value;
    }

    public static int GetRequiredInt(
        IReadOnlyDictionary<string, double> parameters,
        string name,
        int min,
        int max)
    {
        var value = GetRequiredDouble(parameters, name, min, max);
        if (Math.Abs(value - Math.Round(value)) > 0.0001d)
        {
            throw new FilterParameterException($"Parameter '{name}' must be an integer.");
        }

        return (int)Math.Round(value);
    }
}
