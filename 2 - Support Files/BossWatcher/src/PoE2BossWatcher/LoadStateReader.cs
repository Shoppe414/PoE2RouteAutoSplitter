namespace PoE2BossWatcher;

public sealed class LoadStateReader
{
    private readonly string _path;
    private bool _lastKnownLoading;

    public LoadStateReader(string path)
    {
        _path = Path.GetFullPath(path);
    }

    public bool IsLoading()
    {
        try
        {
            if (!File.Exists(_path)) return _lastKnownLoading = false;
            var text = File.ReadAllText(_path).Trim();
            if (text.StartsWith("LOADING", StringComparison.OrdinalIgnoreCase))
                return _lastKnownLoading = true;
            if (text.StartsWith("RUNNING", StringComparison.OrdinalIgnoreCase))
                return _lastKnownLoading = false;
        }
        catch
        {
            // Transient ASL write/read races must not invent a load state. Preserve the last good value.
        }
        return _lastKnownLoading;
    }
}
