namespace KrLensServer.Core.Exceptions;

public sealed class UnsupportedImageFormatException : Exception
{
    public UnsupportedImageFormatException(string format)
        : base($"Unsupported image format '{format}'.")
    {
    }
}
