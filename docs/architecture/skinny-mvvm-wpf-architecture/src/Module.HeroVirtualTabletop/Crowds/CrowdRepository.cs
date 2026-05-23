using System.Text.Json;

namespace Module.HeroVirtualTabletop.Crowds;

/// <summary>
/// Domain class. Owns the crowd collection and all crowd persistence operations:
/// save dirty crowds to their individual source files, save a crowd to a new file,
/// and maintain the active crowd list.
/// The file-system seam (ICrowdFileAccess) is injected so tests stay pure.
/// </summary>
public class CrowdRepository
{
    private readonly ICrowdFileAccess _fileAccess;
    private readonly List<Crowd>      _crowds          = new();
    private readonly List<string>     _activeCrowdList = new();

    public IReadOnlyList<Crowd>  Crowds          => _crowds;
    public IReadOnlyList<string> ActiveCrowdList => _activeCrowdList;

    public CrowdRepository(ICrowdFileAccess fileAccess)
    {
        _fileAccess = fileAccess;
    }

    public void Add(Crowd crowd) => _crowds.Add(crowd);

    /// <summary>
    /// Saves every top-level crowd whose IsDirty is true and whose SourceFile
    /// is non-null. Crowds with no SourceFile are skipped (caller is expected
    /// to prompt the GM with Save Crowd to New File for those).
    /// Returns a SaveResult summarising saved, skipped, and failed crowds.
    /// </summary>
    public SaveResult SaveDirtyCrowds()
    {
        var failures   = new List<SaveFailure>();
        int savedCount = 0;
        int skipped    = 0;

        foreach (var crowd in _crowds.Where(c => c.IsDirty))
        {
            if (crowd.SourceFile is null)
            {
                skipped++;
                continue;
            }

            try
            {
                WriteCrowdToFile(crowd, crowd.SourceFile);
                crowd.RecordSavedTo(crowd.SourceFile);
                savedCount++;
            }
            catch (Exception ex)
            {
                failures.Add(new SaveFailure(crowd.Name, crowd.SourceFile, ex.Message));
            }
        }

        return new SaveResult(failures, savedCount, skipped);
    }

    /// <summary>
    /// Saves the specified top-level crowd to the given path, assigns that path
    /// as the crowd's SourceFile, registers it in the ActiveCrowdList, and
    /// clears the dirty flag.
    /// Throws CrowdSaveException when the write fails.
    /// Throws InvalidOperationException when crowd is nested (not top-level).
    /// </summary>
    public void SaveCrowdToNewFile(Crowd crowd, string path)
    {
        GuardTopLevel(crowd);

        try
        {
            WriteCrowdToFile(crowd, path);
        }
        catch (Exception ex)
        {
            throw new CrowdSaveException(path, ex);
        }

        crowd.RecordSavedTo(path);
        RegisterInActiveCrowdList(path);
    }

    private void GuardTopLevel(Crowd crowd)
    {
        bool isNested = _crowds
            .Any(top => top.NestedCrowds.Contains(crowd));

        if (isNested)
            throw new InvalidOperationException(
                "Save Crowd to New File requires a top-level crowd; the supplied crowd is nested inside another crowd.");
    }

    private void WriteCrowdToFile(Crowd crowd, string path)
    {
        var dto  = CrowdDto.From(crowd);
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        _fileAccess.Write(path, json);
    }

    private void RegisterInActiveCrowdList(string path)
    {
        if (!_activeCrowdList.Contains(path))
            _activeCrowdList.Add(path);
    }
}
