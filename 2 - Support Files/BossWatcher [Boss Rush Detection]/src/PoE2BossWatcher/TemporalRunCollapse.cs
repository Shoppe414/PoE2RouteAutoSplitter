namespace PoE2BossWatcher;

/// <summary>
/// Detects a sudden loss of the horizontal red health-bar run relative to its recent stable
/// history. This intentionally does NOT compare against the run value captured at boss
/// acquisition because normal boss damage makes that baseline stale.
///
/// When a collapse starts, the pre-collapse reference is frozen briefly. This gives the learned
/// name template time to disappear too. If the template remains strongly present for long enough,
/// the collapse is treated as an ordinary health drop and the rolling history is rebased.
/// </summary>
public sealed class TemporalRunCollapse
{
    private readonly AppConfig _config;
    private readonly Queue<RunSample> _history = new();

    private DateTimeOffset? _collapseStarted;
    private double _collapseReference;

    public TemporalRunCollapse(AppConfig config)
    {
        _config = config;
    }

    public bool IsActive => _collapseStarted.HasValue;
    public DateTimeOffset? CollapseStarted => _collapseStarted;
    public double RecentReference { get; private set; }
    public double DropRatio { get; private set; } = 1.0;
    public int HistoryCount => _history.Count;

    public void Reset(DateTimeOffset now, double currentRun)
    {
        _history.Clear();
        _collapseStarted = null;
        _collapseReference = 0;
        RecentReference = currentRun;
        DropRatio = 1.0;
        AddSample(now, currentRun);
    }

    public void Clear()
    {
        _history.Clear();
        _collapseStarted = null;
        _collapseReference = 0;
        RecentReference = 0;
        DropRatio = 1.0;
    }

    public void Update(DateTimeOffset now, double currentRun, double templateCoverage)
    {
        if (_collapseStarted.HasValue)
        {
            RecentReference = _collapseReference;
            DropRatio = Ratio(currentRun, _collapseReference);

            // A strong recovery means the apparent collapse was transient.
            if (_collapseReference > 0 &&
                currentRun >= _collapseReference * _config.TrackedRedRunRecoveryRelativeFraction)
            {
                Rebase(now, currentRun);
                return;
            }

            // If the name template remains strongly present for long enough, this was almost
            // certainly ordinary HP loss rather than the UI disappearing. Rebase to the lower
            // health level so future disappearances are judged against the new recent state.
            if ((now - _collapseStarted.Value).TotalMilliseconds >= _config.TrackedRedRunRebaseMs &&
                templateCoverage >= _config.TrackedRedRunRebaseTemplateCoverage)
            {
                Rebase(now, currentRun);
            }
            return;
        }

        TrimHistory(now);

        if (_history.Count >= _config.TrackedRedRunMinSamples)
            RecentReference = Median(_history.Select(s => s.Value));
        else if (_history.Count > 0)
            RecentReference = Median(_history.Select(s => s.Value));
        else
            RecentReference = currentRun;

        DropRatio = Ratio(currentRun, RecentReference);

        var collapseThreshold = Math.Max(
            _config.TrackedRedRunCollapseAbsoluteFraction,
            RecentReference * _config.TrackedRedRunCollapseRelativeFraction);

        if (_history.Count >= _config.TrackedRedRunMinSamples &&
            RecentReference >= _config.TrackedRedRunReferenceMinFraction &&
            currentRun <= collapseThreshold)
        {
            _collapseStarted = now;
            _collapseReference = RecentReference;
            // Do not add the collapsed sample yet; freeze the recent reference while the
            // learned template gets a chance to corroborate disappearance.
            return;
        }

        AddSample(now, currentRun);
    }

    private void Rebase(DateTimeOffset now, double currentRun)
    {
        _history.Clear();
        _collapseStarted = null;
        _collapseReference = 0;
        RecentReference = currentRun;
        DropRatio = 1.0;
        AddSample(now, currentRun);
    }

    private void AddSample(DateTimeOffset now, double value)
    {
        _history.Enqueue(new RunSample(now, value));
        TrimHistory(now);
    }

    private void TrimHistory(DateTimeOffset now)
    {
        var cutoff = now.AddMilliseconds(-_config.TrackedRedRunLookbackMs);
        while (_history.Count > 0 && _history.Peek().Time < cutoff)
            _history.Dequeue();
    }

    private static double Ratio(double current, double reference)
        => reference <= 0.000001 ? 1.0 : current / reference;

    private static double Median(IEnumerable<double> values)
    {
        var a = values.OrderBy(v => v).ToArray();
        if (a.Length == 0) return 0;
        var mid = a.Length / 2;
        return a.Length % 2 == 1 ? a[mid] : (a[mid - 1] + a[mid]) / 2.0;
    }

    private readonly record struct RunSample(DateTimeOffset Time, double Value);
}
