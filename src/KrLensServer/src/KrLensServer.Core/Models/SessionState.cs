namespace KrLensServer.Core.Models;

public sealed record SessionState(
    string SessionId,
    int Width,
    int Height,
    int CurrentStep,
    int MaxHistory,
    bool CanUndo,
    bool CanRedo,
    IReadOnlyList<FilterHistoryItem> History);
