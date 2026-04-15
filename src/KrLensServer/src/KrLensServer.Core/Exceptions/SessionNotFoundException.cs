namespace KrLensServer.Core.Exceptions;

public sealed class SessionNotFoundException : Exception
{
    public SessionNotFoundException(string sessionId)
        : base($"Session '{sessionId}' was not found.")
    {
    }
}
