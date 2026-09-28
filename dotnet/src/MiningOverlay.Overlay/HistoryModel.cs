namespace MiningOverlay.Overlay;

/// <summary>
/// Port of main.py's `self._history` deque(maxlen=HISTORY_SIZE) — newest first, only
/// unique values kept, oldest silently evicted once the cap is reached. A value that
/// scrolls out can reappear and be re-added (it's a recency window, not a permanent set).
/// </summary>
public sealed class HistoryModel
{
    private readonly int _maxSize;
    private readonly LinkedList<int> _values = new();

    public HistoryModel(int maxSize) => _maxSize = Math.Max(1, maxSize);

    public IReadOnlyList<int> Values => _values.ToArray();

    /// <returns>true if this was a new reading and the history changed.</returns>
    public bool TryAdd(int rs)
    {
        if (_values.Contains(rs))
            return false;

        _values.AddFirst(rs);
        while (_values.Count > _maxSize)
            _values.RemoveLast();
        return true;
    }
}
