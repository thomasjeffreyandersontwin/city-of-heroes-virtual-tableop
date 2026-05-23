namespace Module.HeroVirtualTabletop.Crowds;

/// <summary>
/// Production implementation of ICrowdFileAccess.
/// Delegates directly to System.IO — no domain logic here.
/// </summary>
public class DiskCrowdFileAccess : ICrowdFileAccess
{
    public void Write(string path, string json)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, json, System.Text.Encoding.UTF8);
    }

    public string Read(string path) =>
        File.ReadAllText(path, System.Text.Encoding.UTF8);

    public bool Exists(string path) =>
        File.Exists(path);
}
