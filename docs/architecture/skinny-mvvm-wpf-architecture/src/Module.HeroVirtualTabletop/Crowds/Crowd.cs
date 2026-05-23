using System.Collections.ObjectModel;
using Module.HeroVirtualTabletop.Characters;

namespace Module.HeroVirtualTabletop.Crowds;

/// <summary>
/// Domain class. A named, hierarchical container of crowd members.
/// Tracks its source file (the individual JSON file it was last saved to)
/// and a dirty flag that signals unsaved structural changes.
/// </summary>
public class Crowd
{
    private readonly ObservableCollection<Character> _members      = new();
    private readonly List<Crowd>                     _nestedCrowds = new();

    public string  Name       { get; private set; }
    public string? SourceFile { get; private set; }
    public bool    IsDirty    { get; private set; }

    public IReadOnlyList<Character> Members      => _members;
    public IReadOnlyList<Crowd>     NestedCrowds => _nestedCrowds;

    public Crowd(string name)
    {
        Name    = name;
        IsDirty = true;
    }

    /// <summary>
    /// Factory used by the crowd repository when restoring a crowd from
    /// serialized data. Assigns the persisted source file and marks it clean.
    /// </summary>
    public static Crowd Restore(string name, string? sourceFile, bool isDirty)
    {
        var crowd = new Crowd(name);
        crowd.SourceFile = sourceFile;
        crowd.IsDirty    = isDirty;
        return crowd;
    }

    public void Add(Character character)
    {
        _members.Add(character);
        MarkDirty();
    }

    public void Remove(Character character)
    {
        _members.Remove(character);
        MarkDirty();
    }

    public void Rename(string newName)
    {
        Name = newName;
        MarkDirty();
    }

    public void AddNestedCrowd(Crowd child)
    {
        _nestedCrowds.Add(child);
        MarkDirty();
    }

    /// <summary>
    /// Called by CrowdRepository after a successful write to assign the path
    /// and clear the dirty flag. Not for direct caller use.
    /// </summary>
    internal void RecordSavedTo(string path)
    {
        SourceFile = path;
        IsDirty    = false;
    }

    private void MarkDirty() => IsDirty = true;
}
