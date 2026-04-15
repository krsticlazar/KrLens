using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

public sealed class BrightnessFilter : IFilter
{
    public string Name => "Brightness";

    public BitmapBuffer Apply(BitmapBuffer source, IReadOnlyDictionary<string, double> parameters)
    {
        ArgumentNullException.ThrowIfNull(source);
        var delta = FilterParameterHelper.GetRequiredInt(parameters, "delta", -255, 255);
        return BitmapFilterBasic.Brightness(source, delta);
    }
}
