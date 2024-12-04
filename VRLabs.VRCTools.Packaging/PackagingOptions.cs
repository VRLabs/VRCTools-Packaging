namespace VRLabs.VRCTools.Packaging;

public class PackagingOptions
{
    public required string WorkingDirectory { get; init; }
    public required string OutputDirectory { get; init; }
    public string? ReleaseUrl { get; init; }
    public string? UnityPackageUrl { get; init; }
    public string? Version { get; init; }
    public bool SkipVcc { get; init; }
    public bool SkipUnityPackage { get; init; }
    public bool IsRunningOnGithubActions { get; init; }
    public Dictionary<string, string> CustomFields { get; init; } = [];
}