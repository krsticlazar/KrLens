namespace KrLensServer.Core.Models;

public sealed record FilterHistoryItem(
    int Step,
    string Filter,
    IReadOnlyDictionary<string, double>? Parameters);
