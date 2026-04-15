namespace KrLensServer.Core.Exceptions;

public sealed class FilterParameterException : Exception
{
    public FilterParameterException(string message)
        : base(message)
    {
    }
}
