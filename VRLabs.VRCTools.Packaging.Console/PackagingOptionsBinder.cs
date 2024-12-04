using System.CommandLine;
using System.CommandLine.Binding;

namespace VRLabs.VRCTools.Packaging.Console;

public class PackagingOptionsBinder : BinderBase<PackagingOptions>
{
    private readonly Argument<string> _workingDirectoryArg;
    private readonly Argument<string> _outputDirectoryArg;
    private readonly Option<string?> _releaseUrlOpt;
    private readonly Option<string?> _unityReleaseUrlOpt;
    private readonly Option<string?> _versionOpt;
    private readonly Option<bool> _noVccOpt;
    private readonly Option<bool> _noUnityOpt;
    private readonly Option<bool> _actionOpt;
    private readonly Option<string[]> _customFieldsOpt;
    
    public PackagingOptionsBinder(Argument<string> workingDirectoryArg, Argument<string> outputDirectoryArg, Option<string?> releaseUrlOpt, 
        Option<string?> unityReleaseUrlOpt, Option<string?> versionOpt, Option<bool> noVccOpt, Option<bool> noUnityOpt, Option<bool> actionOpt,
        Option<string[]> customFieldsOpt)
    {
        _workingDirectoryArg = workingDirectoryArg;
        _outputDirectoryArg = outputDirectoryArg;
        _releaseUrlOpt = releaseUrlOpt;
        _unityReleaseUrlOpt = unityReleaseUrlOpt;
        _versionOpt = versionOpt;
        _noVccOpt = noVccOpt;
        _noUnityOpt = noUnityOpt;
        _actionOpt = actionOpt;
        _customFieldsOpt = customFieldsOpt;
    }

    protected override PackagingOptions GetBoundValue(BindingContext bindingContext) =>
        new()
        {
            WorkingDirectory = bindingContext.ParseResult.GetValueForArgument(_workingDirectoryArg),
            OutputDirectory = bindingContext.ParseResult.GetValueForArgument(_outputDirectoryArg),
            ReleaseUrl = bindingContext.ParseResult.GetValueForOption(_releaseUrlOpt),
            UnityPackageUrl = bindingContext.ParseResult.GetValueForOption(_unityReleaseUrlOpt),
            Version = bindingContext.ParseResult.GetValueForOption(_versionOpt),
            SkipVcc = bindingContext.ParseResult.GetValueForOption(_noVccOpt),
            SkipUnityPackage = bindingContext.ParseResult.GetValueForOption(_noUnityOpt),
            IsRunningOnGithubActions = bindingContext.ParseResult.GetValueForOption(_actionOpt),
            CustomFields = GetDictionaryField(bindingContext.ParseResult.GetValueForOption(_customFieldsOpt))
        };

    private Dictionary<string, string> GetDictionaryField(string[]? values)
    {
        var dictionary = new Dictionary<string, string>();

        foreach (var kvp in values ?? [])
        {
            var split = kvp.Split('=');
            if (split.Length >= 2)
            {
                dictionary.Add(split[0], string.Join("=", split[1..]));
            }
        }

        return dictionary;
    }
}