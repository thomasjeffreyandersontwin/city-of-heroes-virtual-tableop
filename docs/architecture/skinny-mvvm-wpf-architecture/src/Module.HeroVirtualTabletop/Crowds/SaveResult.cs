namespace Module.HeroVirtualTabletop.Crowds;

/// <summary>
/// Summary returned by CrowdRepository.SaveDirtyCrowds().
/// Callers inspect Failures to surface per-crowd error messages in the UI.
/// </summary>
public class SaveResult
{
    public IReadOnlyList<SaveFailure> Failures { get; }
    public int SavedCount  { get; }
    public int SkippedCount { get; }

    public SaveResult(IEnumerable<SaveFailure> failures, int savedCount, int skippedCount)
    {
        Failures     = failures.ToList();
        SavedCount   = savedCount;
        SkippedCount = skippedCount;
    }

    public bool HasFailures => Failures.Count > 0;
}

public record SaveFailure(string CrowdName, string Path, string Reason);
// Note: test asserts on .Path — matches the record property name above.
