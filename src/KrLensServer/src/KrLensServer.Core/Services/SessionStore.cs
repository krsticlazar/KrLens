using System.Collections.Concurrent;
using System.Threading.Tasks;
using KrLensServer.Core.Exceptions;
using KrLensServer.Core.Models;

namespace KrLensServer.Core.Services;

public sealed class SessionStore
{
    public const int HistoryLimit = 50;

    private readonly ConcurrentDictionary<string, SessionEntry> _sessions = new(StringComparer.Ordinal);

    public string Create(BitmapBuffer image)
    {
        ArgumentNullException.ThrowIfNull(image);
        var sessionId = Guid.NewGuid().ToString();
        _sessions[sessionId] = new SessionEntry(sessionId, image.Clone());
        return sessionId;
    }

    public BitmapBuffer GetRequired(string sessionId)
    {
        return GetEntry(sessionId).GetCurrent();
    }

    public SessionState GetState(string sessionId)
    {
        return GetEntry(sessionId).GetState();
    }

    public void Push(string sessionId, BitmapBuffer image, string filter, IReadOnlyDictionary<string, double>? parameters)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(filter);
        GetEntry(sessionId).Push(image, filter, parameters);
    }

    public BitmapBuffer Undo(string sessionId)
    {
        return GetEntry(sessionId).Undo();
    }

    public BitmapBuffer Redo(string sessionId)
    {
        return GetEntry(sessionId).Redo();
    }

    public BitmapBuffer Revert(string sessionId)
    {
        return GetEntry(sessionId).Revert();
    }

    public BitmapBuffer RotateRight(string sessionId)
    {
        return GetEntry(sessionId).RotateRight();
    }

    public bool Delete(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _sessions.TryRemove(sessionId, out _);
    }

    private SessionEntry GetEntry(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (_sessions.TryGetValue(sessionId, out var entry))
        {
            return entry;
        }

        throw new SessionNotFoundException(sessionId);
    }

    private sealed class SessionEntry
    {
        private readonly object _sync = new();
        private readonly string _sessionId;
        private readonly List<BitmapBuffer> _snapshots;
        private readonly List<HistoryRecord> _history;
        private int _currentStep;
        private bool _historyNavigationSuspended;

        public SessionEntry(string sessionId, BitmapBuffer original)
        {
            _sessionId = sessionId;
            _snapshots = new List<BitmapBuffer> { original };
            _history = new List<HistoryRecord>();
        }

        public BitmapBuffer GetCurrent()
        {
            lock (_sync)
            {
                return _snapshots[_currentStep];
            }
        }

        public SessionState GetState()
        {
            lock (_sync)
            {
                var current = _snapshots[_currentStep];
                return new SessionState(
                    _sessionId,
                    current.Width,
                    current.Height,
                    _currentStep,
                    HistoryLimit,
                    !_historyNavigationSuspended && _currentStep > 0,
                    !_historyNavigationSuspended && _currentStep < _history.Count,
                    _history
                        .Select((entry, index) => new FilterHistoryItem(index + 1, entry.Filter, entry.Parameters))
                        .ToArray());
            }
        }

        public void Push(BitmapBuffer image, string filter, IReadOnlyDictionary<string, double>? parameters)
        {
            lock (_sync)
            {
                _historyNavigationSuspended = false;

                if (_currentStep < _history.Count)
                {
                    _history.RemoveRange(_currentStep, _history.Count - _currentStep);
                    _snapshots.RemoveRange(_currentStep + 1, _snapshots.Count - (_currentStep + 1));
                }

                _snapshots.Add(image.Clone());
                _history.Add(new HistoryRecord(filter, CopyParameters(parameters)));
                _currentStep = _history.Count;

                if (_history.Count > HistoryLimit)
                {
                    _history.RemoveAt(0);
                    _snapshots.RemoveAt(1);
                    _currentStep = Math.Max(0, _currentStep - 1);
                }
            }
        }

        public BitmapBuffer Undo()
        {
            lock (_sync)
            {
                _historyNavigationSuspended = false;
                if (_currentStep > 0)
                {
                    --_currentStep;
                }

                return _snapshots[_currentStep];
            }
        }

        public BitmapBuffer Redo()
        {
            lock (_sync)
            {
                _historyNavigationSuspended = false;
                if (_currentStep < _history.Count)
                {
                    ++_currentStep;
                }

                return _snapshots[_currentStep];
            }
        }

        public BitmapBuffer Revert()
        {
            lock (_sync)
            {
                _historyNavigationSuspended = false;
                _currentStep = 0;
                return _snapshots[0];
            }
        }

        public BitmapBuffer RotateRight()
        {
            lock (_sync)
            {
                for (var index = 0; index < _snapshots.Count; ++index)
                {
                    _snapshots[index] = RotateClockwise(_snapshots[index]);
                }

                _historyNavigationSuspended = true;
                return _snapshots[_currentStep];
            }
        }

        private static IReadOnlyDictionary<string, double>? CopyParameters(IReadOnlyDictionary<string, double>? parameters)
        {
            if (parameters is null || parameters.Count == 0)
            {
                return null;
            }

            return parameters.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        }

        private static BitmapBuffer RotateClockwise(BitmapBuffer source)
        {
            var destination = new BitmapBuffer(source.Height, source.Width, source.Channels);
            var channels = source.Channels;

            Parallel.For(0, source.Height, y =>
            {
                for (var x = 0; x < source.Width; ++x)
                {
                    var sourceOffset = source.GetOffset(x, y);
                    var destinationX = source.Height - 1 - y;
                    var destinationY = x;
                    var destinationOffset = ((destinationY * destination.Width) + destinationX) * channels;
                    Buffer.BlockCopy(source.Pixels, sourceOffset, destination.Pixels, destinationOffset, channels);
                }
            });

            return destination;
        }

        private sealed record HistoryRecord(string Filter, IReadOnlyDictionary<string, double>? Parameters);
    }
}
