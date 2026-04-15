namespace KrLensServer.Core.Exceptions;

public sealed class MsiCorruptedException : Exception
{
    public MsiCorruptedException(string message)
        : base(message)
    {
    }
}
