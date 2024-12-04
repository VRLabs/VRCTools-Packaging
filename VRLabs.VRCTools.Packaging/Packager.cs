using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.FileSystemGlobbing;
using Serilog;

namespace VRLabs.VRCTools.Packaging;

public static class Packager
{
    public static async Task<bool> CreatePackage(PackagingOptions options)
    {
        if (options.SkipVcc && options.SkipUnityPackage)
        {
            Log.Information("Both skipVcc and skipUnityPackage are true, nothing to do");
            return true;
        }
        var data = GetPackageJson(options.WorkingDirectory, options.SkipUnityPackage);
        if (data == null)
        {
            Log.Error("Could not find valid package.json in {WorkingDirectory}", options.WorkingDirectory);
            return false;
        }
        
        foreach (var keyValuePair in options.CustomFields)
        {
            data[keyValuePair.Key] = keyValuePair.Value;
        }
        
        string tempPath = Path.GetTempPath();
        tempPath += "/VRLabs/Packaging";
        if(Directory.Exists(tempPath)) DeleteDirectory(tempPath);
        Directory.CreateDirectory(tempPath);
        
        if(!Directory.Exists(options.OutputDirectory)) Directory.CreateDirectory(options.OutputDirectory);
        
        string? sha256String = null;
        data.Remove("zipSHA256");

        string packageName = data["name"]!.ToString();
        
        if(!string.IsNullOrEmpty(options.Version))
            data["version"] = options.Version;

        string packageVersion = data["version"]!.ToString();
        
        StringBuilder githubOutput = new();

        // VCC Package Creation
        if (!options.SkipVcc)
        {
            if(!string.IsNullOrEmpty(options.ReleaseUrl))
                data["url"] = options.ReleaseUrl;
            
            CopyDirectory(options.WorkingDirectory, tempPath, true, true);
            string outputFileName = $"{packageName}-{packageVersion}.zip";
            string outputFilePath = $"{options.OutputDirectory}/{outputFileName}";

            string jsonPath = tempPath + "/package.json";
            if (File.Exists(jsonPath))
            {
                await File.WriteAllTextAsync(jsonPath, data.ToJsonString(new JsonSerializerOptions{ DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }));
                Log.Information("Zipping {WorkingDirectory}", tempPath);
                if(File.Exists(outputFilePath))
                    File.Delete(outputFilePath);
                
                var assetMatcher = new Matcher();
                AddExporterPatterns(assetMatcher);
                
                string[] matchedAssets = assetMatcher.GetResultsInFullPath(tempPath).ToArray();
                
                CreateZipFile(tempPath, outputFilePath, matchedAssets.ToList());
                
                Log.Information("Finished Zipping, available at {OutputFilePath}", outputFilePath);
                if(options.IsRunningOnGithubActions)
                {
                    githubOutput.AppendLine($"vccPackagePath={outputFilePath}");
                }

                using var sha256 = SHA256.Create();
                await using var stream = File.OpenRead(outputFilePath);
                byte[] hash = await sha256.ComputeHashAsync(stream);
                sha256String = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
            DeleteDirectory(tempPath);
        }

        // Unity Package Creation
        if (!options.SkipUnityPackage)
        {
            string unityPackageDestinationFolder = data["unityPackageDestinationFolder"]?.ToString() ?? $"Assets/{data["name"]}";
            
            if (!string.IsNullOrEmpty(options.UnityPackageUrl))
                data["unityPackageUrl"] = options.UnityPackageUrl;

            var folderMetas = data["unityPackageDestinationFolderMetas"].Deserialize<Dictionary<string, string>>();
            CreateExtraFolders(tempPath, folderMetas);

            CopyDirectory(options.WorkingDirectory, tempPath + "/" + unityPackageDestinationFolder, true);
            
            string outputFileName = $"{packageName}-{packageVersion}.unitypackage";
            string outputFilePath = $"{options.OutputDirectory}/{outputFileName}";
        
            string jsonPath = tempPath + "/" + unityPackageDestinationFolder + "/package.json";
            if (File.Exists(jsonPath))
            {
                await File.WriteAllTextAsync(jsonPath, data.ToJsonString(new JsonSerializerOptions{ DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }));
            }
            Log.Information("Packaging {WorkingDirectory}", tempPath);
            
            if(File.Exists(outputFilePath))
                File.Delete(outputFilePath);

            // Make the output file (touch it) so we can exclude
            await File.WriteAllBytesAsync(outputFilePath, Array.Empty<byte>());
            
            await using var packer = new unityPackagePacker(tempPath, outputFilePath);

            // Match all the assets we need
            var assetMatcher = new Matcher();
            AddExporterPatterns(assetMatcher);
            assetMatcher.AddExclude(outputFileName);

            IEnumerable<string> matchedAssets = assetMatcher.GetResultsInFullPath(tempPath);
            
            // Download the icon if it's a valid url and add it to the package
            string? icon = data["icon"]?.ToString();
            if (!string.IsNullOrEmpty(icon) && Uri.TryCreate(icon, UriKind.Absolute, out Uri _))
            {
                Log.Information("Downloading icon from {IconUrl}", icon);
                using var client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(icon);
                response.EnsureSuccessStatusCode();
                byte[] responseBody = await response.Content.ReadAsByteArrayAsync();
                // check if the icon is a valid png by checking first 8 bytes
                if (responseBody.Length < 8 || !responseBody.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
                {
                    Log.Warning("Icon at {IconUrl} is not a valid PNG package will be generated without an icon", icon);
                }
                
                await File.WriteAllBytesAsync($"{tempPath}/.icon.png", responseBody);
                Log.Information("Finished downloading icon at {IconPath}", $"{tempPath}/.icon.png");
                await packer.AddAssetAsync($"{tempPath}/.icon.png");
            }
            else
            {
                Log.Information("No icon found, skipping icon download");
            }
            
            await packer.AddAssetsAsync(matchedAssets);
            await packer.FlushAsync();
            
            Log.Information("Finished Packaging, available at {OutputFilePath}", outputFilePath);
            if(options.IsRunningOnGithubActions)
            {
                githubOutput.AppendLine($"unityPackagePath={outputFilePath}");
            }
            DeleteDirectory(tempPath);
        }
        
        if(sha256String is not null)
            data["zipSHA256"] = sha256String;
        
        var serverPackageJsonPath = $"{options.OutputDirectory}/server-package.json"; 
        
        await File.WriteAllTextAsync(serverPackageJsonPath, JsonSerializer.Serialize(data));
        Log.Information("Finished creating server-package.json, available at {OutputFilePath}", serverPackageJsonPath);
        if(options.IsRunningOnGithubActions)
        {
            githubOutput.AppendLine($"serverPackageJsonPath={serverPackageJsonPath}");

            string variables = Environment.GetEnvironmentVariable("GITHUB_OUTPUT")!;
            await File.AppendAllTextAsync(variables, githubOutput.ToString());
        }
        
        return true;
    }

    private static void AddExporterPatterns(Matcher assetMatcher)
    {
        assetMatcher.AddIncludePatterns(new[] { "**.*" });
        assetMatcher.AddExcludePatterns(new[] { "**/.*" });
    }

    private static JsonObject? GetPackageJson(string workingDirectory, bool skipUnityPackage)
    {
        string packagePath = workingDirectory + "/package.json";
        if (!File.Exists(packagePath)) return null;

        var package = JsonNode.Parse(File.ReadAllText(packagePath))?.AsObject();// JsonSerializer.Deserialize<VrlPackageJson>(File.ReadAllText(packagePath), new JsonSerializerOptions{PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
        var failures = new List<string>();
        
        failures.IsFieldNullOrEmpty(package, "name", "package.json is missing name");
        failures.IsFieldNullOrEmpty(package, "version", "package.json is missing version");
        failures.IsFieldNullOrEmpty(package, "displayName", "package.json is missing displayName");
        failures.IsFieldNullOrEmpty(package, "description", "package.json is missing description");
        failures.IsFieldNullOrEmpty(package, "author", "package.json is missing author");
        //failures.IsFieldNullOrEmpty(package, "unity", "package.json is missing unity version");

        if (!skipUnityPackage)
        {
            failures.IsFieldNullOrEmpty(package, "unityPackageDestinationFolder", "package.json is missing unityPackageDestinationFolder");
            failures.IsFieldNullOrEmpty(package, "unityPackageDestinationFolderMetas", "package.json is missing unityPackageDestinationFolderMetas");
        }
        
        if (failures.Count > 0)
        {
            Log.Error("package.json is invalid");
            foreach (var error in failures)
            {
                Log.Error(" -{ValidationError}",error);
            }
            return null;
        }
        
        return package;
    }
    
    

    private static void CreateExtraFolders(string path, Dictionary<string, string>? folders)
    {
        if (folders is null) return;
        foreach (var folder in folders)
        {
            Directory.CreateDirectory($"{path}/{folder.Key}");
            File.WriteAllText($"{path}/{folder.Key}.meta", "fileFormatVersion: 2\nguid: " + folder.Value + "\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: ");
        }
    }
    
    private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive, bool ignoreDotPaths = false)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            if(ignoreDotPaths && file.Name.StartsWith(".")) continue;
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }
        
        if (!recursive) return;
        // If recursive and copying subdirectories, recursively call this method
        foreach (DirectoryInfo subDir in dirs)
        {
            if(ignoreDotPaths && subDir.Name.StartsWith(".")) continue;
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir, true, ignoreDotPaths);
        }
    }

    private static void DeleteDirectory(string targetDir)
    {
        File.SetAttributes(targetDir, FileAttributes.Normal);

        string[] files = Directory.GetFiles(targetDir);
        string[] dirs = Directory.GetDirectories(targetDir);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
        {
            DeleteDirectory(dir);
        }
        
        Directory.Delete(targetDir, false);
    }

    private static void CreateZipFile(string entryDirectoryPath, string outputPath, List<string> filePaths)
    {
        using var zipStream = new ZipOutputStream(File.Create(outputPath));
        zipStream.SetLevel(9); // Set the compression level (0-9)

        byte[] buffer = new byte[4096];

        foreach (string filePath in filePaths)
        {
            var file = new FileInfo(filePath);
            if (!file.Exists) continue;

            string relativePath = Path.GetRelativePath(entryDirectoryPath, filePath);
            
            if (file.Extension == ".meta")
            {
                if (File.Exists(file.FullName[..^5]))
                {
                    Log.Information("Writing meta {Path}", relativePath);
                    WriteFile(relativePath, filePath, zipStream, buffer);
                    continue;
                }
                if(Directory.Exists(file.FullName[..^5]))
                {
                    Log.Information("Writing meta {Path}", relativePath);
                    WriteFile(relativePath, filePath, zipStream, buffer);
                    continue;
                }
                
                Log.Warning("{Path} refers to a non existing file/folder, skipping", relativePath);
                continue;
            }
            
            string metaFile = $"{file.FullName}.meta";
            if (!File.Exists(metaFile))
            {
                //Meta file is missing so we will skip it.
                Log.Warning("Missing .meta for {Path}, skipping", relativePath);
                continue;
            }
            
            Log.Information("Writing File {Path}", relativePath);
            WriteFile(relativePath, filePath, zipStream, buffer);
        }

        zipStream.Finish();
        zipStream.Close();
    }

    private static void WriteFile(string relativePath, string filePath, ZipOutputStream zipStream, byte[] buffer)
    {
        var entry = new ZipEntry(relativePath);
        entry.DateTime = DateTime.Now;
        zipStream.PutNextEntry(entry);

        using FileStream fileStream = File.OpenRead(filePath);
        int bytesRead;
        do
        {
            bytesRead = fileStream.Read(buffer, 0, buffer.Length);
            zipStream.Write(buffer, 0, bytesRead);
        } while (bytesRead > 0);
    }
}