namespace KrLensServer.Core.Filters;

internal static class FilterMath
{
    public static byte ClampToByte(double value) => (byte)Math.Clamp((int)Math.Round(value), 0, 255);

    public static int ClampCoordinate(int value, int maxInclusive) => Math.Clamp(value, 0, maxInclusive);

    public static byte GetLuminance(byte r, byte g, byte b)
    {
        var luminance = (0.299d * r) + (0.587d * g) + (0.114d * b);
        return ClampToByte(luminance);
    }
}
