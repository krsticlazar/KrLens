using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

public sealed class WaterFilter : IFilter
{
    public string Name => "Water";

    public BitmapBuffer Apply(BitmapBuffer source, IReadOnlyDictionary<string, double> parameters)
    {
        ArgumentNullException.ThrowIfNull(source);
        var amplitude = FilterParameterHelper.GetRequiredDouble(parameters, "amplitude", 0d, 100d);
        var wavelength = FilterParameterHelper.GetRequiredDouble(parameters, "wavelength", 1d, 1000d);
        return BitmapFilterDisplacement.Water(source, amplitude, wavelength);
    }
}
