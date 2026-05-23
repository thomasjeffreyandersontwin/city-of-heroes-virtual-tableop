using Module.HeroVirtualTabletop.Crowds;

namespace Module.UnitTest.Domain;

/// <summary>
/// Test double: in-memory file system for crowd persistence tests.
/// Pre-seed FailOnPath to simulate write failures.
/// Inspect WrittenPaths / WrittenContent after exercising the domain code.
/// </summary>
public class FakeCrowdFileAccess : ICrowdFileAccess
{
    public List<string>                    WrittenPaths   { get; } = new();
    public Dictionary<string, string>      WrittenContent { get; } = new();
    public string?                         FailOnPath     { get; set; }
    public Dictionary<string, string>      ExistingFiles  { get; } = new();

    public void Write(string path, string json)
    {
        if (path == FailOnPath)
            throw new IOException($"Simulated write failure for path: {path}");

        WrittenPaths.Add(path);
        WrittenContent[path] = json;
    }

    public string Read(string path)
    {
        if (!ExistingFiles.TryGetValue(path, out var content))
            throw new FileNotFoundException($"Simulated: file not found: {path}");
        return content;
    }

    public bool Exists(string path) => ExistingFiles.ContainsKey(path);
}
