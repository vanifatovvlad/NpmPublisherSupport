using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NpmPublisherSupport
{
    public enum NpmVersion : byte
    {
        Major,
        Minor,
        Patch,
    }

    public class NpmCommands
    {
        public static void Publish(NpmCommandCallback action, string registry)
        {
            if (string.IsNullOrEmpty(registry))
            {
                NpmUtils.ExecuteNpmCommand($"publish ", action);
            }
            else
            {
                NpmUtils.ExecuteNpmCommand($"publish --registry {registry}", action);
            }
        }

        public static void UpdatePatchVersionRecursively(TextAsset package)
        {
            UpdateVersionRecursively(package, NpmVersion.Patch);
        }

        public static void UpdateVersionRecursively(TextAsset package, NpmVersion version)
        {
            try
            {
                var localPackages = UpmClientUtils.FindLocalPackages()
                    .Select(pkg => new
                    {
                        package = pkg,
                        json = (Dictionary<string, object>) MiniJSON.Json.Deserialize(pkg.text),
                    })
                    .ToList();

                var packageJson = JsonUtility.FromJson<Package>(package.text);
                var packageJsonNewVersion = SemVerHelper.GenerateVersion(packageJson.version, version);

                var newVersions = new Dictionary<string, string>
                {
                    {packageJson.name, packageJsonNewVersion}
                };

                localPackages.Single(o => o.package == package).json["version"] = packageJsonNewVersion;

                var patchInfos = new List<Tuple<string, string, string>>();
                
                bool dirty = true;
                int limit = 1000;
                while (dirty && limit-- > 0)
                {
                    dirty = false;
                    foreach (var current in localPackages)
                    {
                        if (!current.json.ContainsKey("dependencies"))
                            continue;

                        var currentName = (string) current.json["name"];
                        var currentVersion = (string) current.json["version"];
                        var currentDepsJson = (Dictionary<string, object>) current.json["dependencies"];

                        foreach (var newVer in newVersions)
                        {
                            if (!currentDepsJson.ContainsKey(newVer.Key) ||
                                (string)currentDepsJson[newVer.Key] == newVer.Value)
                                continue;

                            currentDepsJson[newVer.Key] = newVer.Value;

                            if (!newVersions.ContainsKey(currentName))
                            {
                                var oldVersion = currentVersion;
                                
                                currentVersion = SemVerHelper.GenerateVersion(currentVersion, NpmVersion.Patch);
                                current.json["version"] = currentVersion;
                                newVersions.Add(currentName, currentVersion);
                                
                                patchInfos.Add(Tuple.Create(currentName, oldVersion, currentVersion));
                            }

                            dirty = true;
                            break;
                        }
                    }
                }

                if (limit == 0)
                {
                    Debug.LogError("UpdateVersionRecursively: Max recursion limit reached");
                    return;
                }

                var nl = Environment.NewLine;
                var msg = $"Following package version would be updated:" +
                          $"{nl}- {packageJson.name}: {packageJson.version} -> {packageJsonNewVersion}" +
                          $"{nl}" +
                          $"{nl}Following dependent packages would be patched:" +
                          patchInfos.Aggregate("", (s, c) => s + $"{nl}- {c.Item1}: {c.Item2} -> {c.Item3}");

                if (EditorUtility.DisplayDialog("Update versions?", msg, "Update", "Cancel"))
                {
                    foreach (var current in localPackages)
                    {
                        var currentName = (string) current.json["name"];
                        if (!newVersions.ContainsKey(currentName))
                            continue;

                        var packageContent = MiniJSON.Json.Serialize(current.json);
                        var path = AssetDatabase.GetAssetPath(current.package);
                        File.WriteAllText(path, packageContent);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void UpdateVersion(TextAsset package, NpmVersion version)
        {
            try
            {
                var json = (Dictionary<string, object>) MiniJSON.Json.Deserialize(package.text);
                var packageName = (string) json["name"];
                var versionString = (string) json["version"];

                var newVersionString = SemVerHelper.GenerateVersion(versionString, version);

                var msg = $"Following package version would be updated:" +
                          $"{Environment.NewLine}- {packageName}: {versionString} -> {newVersionString}";

                if (EditorUtility.DisplayDialog("Update version?", msg, "Update", "Cancel"))
                {
                    json["version"] = newVersionString;

                    var packageContent = MiniJSON.Json.Serialize(json);
                    var path = AssetDatabase.GetAssetPath(package);
                    File.WriteAllText(path, packageContent);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void SetDependencyVersion(TextAsset package, string depName, string depVersion)
        {
            try
            {
                var json = (Dictionary<string, object>) MiniJSON.Json.Deserialize(package.text);
                var depsJson = (Dictionary<string, object>) json["dependencies"];
                depsJson[depName] = depVersion;

                var packageContent = MiniJSON.Json.Serialize(json);
                var path = AssetDatabase.GetAssetPath(package);
                File.WriteAllText(path, packageContent);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        
        public static string GetDependencyVersion(TextAsset package, string depName)
        {
            try
            {
                var json = (Dictionary<string, object>) MiniJSON.Json.Deserialize(package.text);

                if (!json.ContainsKey("dependencies"))
                    return null;
                
                var depsJson = (Dictionary<string, object>) json["dependencies"];
                if (!depsJson.ContainsKey(depName))
                    return null;
                
                return (string)depsJson[depName];
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        public static string GetPackageDirectory(TextAsset package)
        {
            var assetPath = AssetDatabase.GetAssetPath(package);
            var path = Application.dataPath;
            path = path.Substring(0, path.Length - "Assets".Length);
            path = path + AssetDatabase.GetAssetPath(package);
            var fileName = Path.GetFileName(assetPath);
            path = path.Substring(0, path.Length - fileName.Length);
            var directory = Path.GetFullPath(path);
            return directory;
        }

        public static void SetWorkingDirectory(TextAsset package)
        {
            var directory = GetPackageDirectory(package);
            NpmUtils.WorkingDirectory = directory;
        }
    }
}