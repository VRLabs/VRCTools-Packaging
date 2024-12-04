using System.CommandLine;
using Serilog;
using VRLabs.VRCTools.Packaging;
using VRLabs.VRCTools.Packaging.Console;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
    
var pathArg = new Argument<string>(name: "path", description: "Package path");
var outputArg = new Argument<string>(name: "output", description: "Output directory path");
var releaseUrlOpt = new Option<string?>(name: "--releaseUrl", getDefaultValue: () => "", description: "Url of the release");
var unityReleaseUrlOpt = new Option<string?>(name: "--unityReleaseUrl", getDefaultValue: () => "", description: "Url of the release of the unitypackage");
var versionOpt = new Option<string?>(name: "--releaseVersion", getDefaultValue: () => "", description: "Version to use for the release, if not specified it will be taken from the package.json");
var noVccOpt = new Option<bool>(name: "--novcc", getDefaultValue: () => false, description: "don't build the vcc zip file");
var noUnityOpt = new Option<bool>(name: "--nounity", getDefaultValue: () => false, description: "don't build the unitypackage");
var actionOpt = new Option<bool>(name: "--action", getDefaultValue: () => false, description: "is it running on github actions?");
var customFieldsOpt = new Option<string[]>(name: "--customJsonFields", getDefaultValue: () => [], description: "custom json fields to add to the package.json"){AllowMultipleArgumentsPerToken = true};

var command = new RootCommand("Packs the assets inside a folder in a Unity Project based on an info file")
{
    pathArg,
    outputArg,
    releaseUrlOpt,
    unityReleaseUrlOpt,
    versionOpt,
    noVccOpt,
    noUnityOpt,
    actionOpt,
    customFieldsOpt
};

command.SetHandler(async (packagingOptions) =>
{
    try
    {
        var result = await Packager.CreatePackage(packagingOptions);
        if (!result)
        {
            Log.Error("Failed to create package");
            Environment.Exit(1);
        }
    }
    catch (Exception e)
    {
        Log.Error(e, "Failed to create package");
        Environment.Exit(1);
    }
    
}, new PackagingOptionsBinder(pathArg, outputArg, releaseUrlOpt, unityReleaseUrlOpt, versionOpt, noVccOpt, noUnityOpt, actionOpt, customFieldsOpt));

command.Invoke(args);