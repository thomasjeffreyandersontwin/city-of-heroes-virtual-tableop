namespace Module.HeroVirtualTabletop.Crowds;

/// <summary>
/// COH Game Bridge seam for crowd file I/O.
/// Production implementation writes to disk; tests use FakeCrowdFileAccess.
/// </summary>
public interface ICrowdFileAccess
{
    void   Write(string path, string json);
    string Read(string path);
    bool   Exists(string path);
}
