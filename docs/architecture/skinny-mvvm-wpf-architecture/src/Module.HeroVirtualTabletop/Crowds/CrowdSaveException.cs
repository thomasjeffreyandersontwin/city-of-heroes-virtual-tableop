namespace Module.HeroVirtualTabletop.Crowds;

/// <summary>
/// Raised when a crowd file write fails during Save Crowd to New File.
/// Contains the target path and the underlying cause.
/// </summary>
public class CrowdSaveException : Exception
{
    public string TargetPath { get; }

    public CrowdSaveException(string targetPath, Exception inner)
        : base($"Failed to save crowd to '{targetPath}': {inner.Message}", inner)
    {
        TargetPath = targetPath;
    }
}
