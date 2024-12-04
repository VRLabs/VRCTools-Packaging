<div align="center">

# VRC Packaging Tool

[![Generic badge](https://img.shields.io/github/downloads/VRLabs/VRCTools-Packaging/total?label=Downloads)](https://github.com/VRLabs/VRCTools-Packaging/releases/latest)
[![Generic badge](https://img.shields.io/badge/License-MIT-informational.svg)](https://github.com/VRLabs/VRCTools-Packaging/blob/main/LICENSE)

[![Generic badge](https://img.shields.io/discord/706913824607043605?color=%237289da&label=DISCORD&logo=Discord&style=for-the-badge)](https://discord.vrlabs.dev/)
[![Generic badge](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Fshieldsio-patreon.vercel.app%2Fapi%3Fusername%3Dvrlabs%26type%3Dpatrons&style=for-the-badge)](https://patreon.vrlabs.dev/)

Console tools to package Unity assets in both `unitypackage` and `vcc` package formats. It's based on [Lachee's](https://github.com/Lachee) [Unity Package Exporter](https://github.com/Lachee/Unity-Package-Exporter)

### ⬇️ [Download latest Release](https://github.com/VRLabs/VRCTools-Packaging/releases/latest)

</div>

---

## How it works

This console tool uses informations provided by the `package.json` file to build the packages. It will also automatically generate a `server-package.json` file that contains some more informations compared to the one included in the packages, useful when setting up custom repository listings.

## Usage

Example usage:

```
VRCPackagingTool.exe "packageAssetsPath" "outputDirectorypath" --releaseUrl "vccReleaseUrl" --unityReleaseUrl "unityReleaseUrl"   
```

Only generating the `vcc` package:

```
VRCPackagingTool.exe "packageAssetsPath" "outputDirectorypath" --releaseUrl "vccReleaseUrl" --nounity  
```

With custom json fields in the `package.json`,useful for custom vcc clients that may use additional fields (for example [ALCOM's](https://vrc-get.anatawa12.com/alcom/) custom changelog url field):

```
VRCPackagingTool.exe "packageAssetsPath" "outputDirectorypath" --releaseUrl "vccReleaseUrl" --unityReleaseUrl "unityReleaseUrl" --customJsonFields "changelogUrl=https://link.to.changelog" "anotherField=anotherValue"
```

You can use the `--help` or `-h` flag to get a list of all the available options.

```
VRCPackagingTool.exe --help       
Description:                                                                                                
  Packs the assets inside a folder in a Unity Project based on an info file

Usage:
  VRLabs.VRCTools.Packaging.Console <path> <output> [options]

Arguments:
  <path>    Package path
  <output>  Output directory path

Options:
  --releaseUrl <releaseUrl>              Url of the release []
  --unityReleaseUrl <unityReleaseUrl>    Url of the release of the unitypackage []
  --releaseVersion <releaseVersion>      Version to use for the release, if not specified it will be taken from the package.json []
  --novcc                                don't build the vcc zip file [default: False]
  --nounity                              don't build the unitypackage [default: False]
  --action                               is it running on github actions? [default: False]
  --customJsonFields <customJsonFields>  custom json fields to add to the package.json []
  --version                              Show version information
  -?, -h, --help                         Show help and usage information
```



## Package.json additions

The tool can use some additional fields in the `package.json` for the packaging process:

| Field                                 | Description                                                                                                                                  |
|---------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------|
| `icon`                                | Url to the icon to use for the UnityPackage, no icons will be added if not there                                                             |
| `unityPackageDestinationFolder`*      | Path where to place the assets inside the UnityPackage, this allows you to not need to store the entire path from Assets in the repository   |
| `unitypackageDestinationFolderMetas`* | Dictionary of folders with their respective metas to be added when generating the UnityPackage, in the format of "Assets/path": "FolderGUID" |

(Fields marked with * are required for the UnityPackage generation)

And it adds some additional fields to the `package.json` (on top of the one passed via the `--customJsonFields` option):

| Field             | Description                                                        |
|-------------------|--------------------------------------------------------------------|
| `unityPackageUrl` | Url of the unitypackage, only added if a UnityPackage is generated |

The tool will also generate a `server-package.json` file (not included in the packages) that can be used to setup custom repository listings, it contains the following extra fields:

| Field       | Description                                                                                                                                                                   |
|-------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `zipSHA256` | Sha256 of the vcc package, this is usually used by the vcc to verify the integrity of the downloaded vcc package, and should be provided for each package by package listings |

## License

VRC Packaging Tool is available as-is under MIT. For more information see [LICENSE](https://github.com/VRLabs/VRCTools-Packaging/blob/main/LICENSE).

<div align="center">

[<img src="https://github.com/VRLabs/Resources/raw/main/Icons/VRLabs.png" width="50" height="50">](https://vrlabs.dev "VRLabs")
<img src="https://github.com/VRLabs/Resources/raw/main/Icons/Empty.png" width="10">
[<img src="https://github.com/VRLabs/Resources/raw/main/Icons/Discord.png" width="50" height="50">](https://discord.vrlabs.dev/ "VRLabs")
<img src="https://github.com/VRLabs/Resources/raw/main/Icons/Empty.png" width="10">
[<img src="https://github.com/VRLabs/Resources/raw/main/Icons/Patreon.png" width="50" height="50">](https://patreon.vrlabs.dev/ "VRLabs")
<img src="https://github.com/VRLabs/Resources/raw/main/Icons/Empty.png" width="10">
[<img src="https://github.com/VRLabs/Resources/raw/main/Icons/Twitter.png" width="50" height="50">](https://twitter.com/vrlabsdev "VRLabs")

</div>
