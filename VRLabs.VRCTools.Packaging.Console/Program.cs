﻿using System.CommandLine;
using Serilog;
using VRLabs.VRCTools.Packaging;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
    
var pathArg = new Argument<string>(name: "path", description: "Unity asset path");
var outputArg = new Argument<string>(name: "output", description: "Output directory path for the packages");
var releaseUrlOpt = new Option<string?>(name: "--releaseUrl", getDefaultValue: () => null, description: "Url of the release");
var unityReleaseUrlOpt = new Option<string?>(name: "--unityReleaseUrl", getDefaultValue: () => null, description: "Url of the release of the unitypackage");
var noVccOpt = new Option<bool>(name: "--novcc", getDefaultValue: () => false, description: "don't build the vcc zip file");
var noUnityOpt = new Option<bool>(name: "--nounity", getDefaultValue: () => false, description: "don't build the unitypackage");
var actionOpt = new Option<bool>(name: "--action", getDefaultValue: () => false, description: "is it running on github actions?");

var command = new RootCommand("Packs the assets inside a folder in a Unity Project based on an info file")
{
    pathArg,
    outputArg,
    releaseUrlOpt,
    unityReleaseUrlOpt,
    noVccOpt,
    noUnityOpt,
    actionOpt
};

command.SetHandler(async (source, output, releaseUrl, unityReleaseUrl, noVcc, noUnity, action) =>
{
    Environment.SetEnvironmentVariable("RUNNING_ON_GITHUB_ACTIONS", action ? "true" : "false");
    var result = await Packager.CreatePackage(source, output, releaseUrl, unityReleaseUrl, noVcc, noUnity);
    if (!result)
    {
        Log.Error("Failed to create package");
        Environment.Exit(1);
    }
    
}, pathArg, outputArg, releaseUrlOpt, unityReleaseUrlOpt, noVccOpt, noUnityOpt, actionOpt);

command.Invoke(args);