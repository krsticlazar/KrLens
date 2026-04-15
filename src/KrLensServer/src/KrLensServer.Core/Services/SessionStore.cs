using System.Collections.Concurrent;
using KrLensServer.Core.Exceptions;
using KrLensServer.Core.Models;

namespace KrLensServer.Core.Services;

public sealed class SessionStore
{
    private readonly ConcurrentDictionary<string, SessionEntry> _sessions = new(StringComparer.Ordinal);

    public string Create(BitmapBuffer image)
    {
        ArgumentNullException.ThrowIfNull(image);
        var sessionId = Guid.NewGuid().ToString();
        _sessions[sessionId] = new SessionEntry(image, DateTimeOffset.UtcNow);
        return sessionId;
    }

    public BitmapBuffer GetRequired(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (_sessions.TryGetValue(sessionId, out var entry))
        {
            _sessions[sessionId] = entry with { LastAccessedUtc = DateTimeOffset.UtcNow };
            return entry.Image;
        }

        throw new SessionNotFoundException(sessionId);
    }

    public void Update(string sessionId, BitmapBuffer image)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(image);

        if (!_sessions.ContainsKey(sessionId))
        {
            throw new SessionNotFoundException(sessionId);
        }

        _sessions[sessionId] = new SessionEntry(image, DateTimeOffset.UtcNow);
    }

    public bool Delete(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _sessions.TryRemove(sessionId, out _);
    }

    private sealed record SessionEntry(BitmapBuffer Image, DateTimeOffset LastAccessedUtc);
}
