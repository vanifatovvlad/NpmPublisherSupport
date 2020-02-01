using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using System.Linq;
using MiniJSON;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace NpmPublisherSupport
{
    public static class NpmPublishMenu
    {
        private const string PublishMenuItemPath = "Assets/View Npm Package";

        //private const string PublishAllSelectedMenu = "Assets/NPM/Publish ALL Selected";
        //private const string PatchAndPublishAllSelectedMenu = "Assets/NPM/Publish and Patch ALL Selected";
        private const string PublishModifiedMenu = "Assets/NPM/Publish Modified";
        private static readonly string[] PackageJsonPaths = {"/package.json", "/Sources/package.json"};

        [MenuItem(PublishMenuItemPath, true)]
        public static bool CanOpenPublishWindow() => GetSelectedPackageJson() != null;

        [MenuItem(PublishMenuItemPath, priority = 2000)]
        public static void OpenPublishWindow()
        {
            var packageJson = GetSelectedPackageJson();
            NpmPublishWindow.OpenPublish(packageJson);
        }

        /*
        [MenuItem(PublishAllSelectedMenu, true)]
        public static bool CanPublishAllSelected() => !string.IsNullOrEmpty(NpmPublishWindow.Registry);

        [MenuItem(PublishAllSelectedMenu, priority = 2000)]
        public static void PublishAllSelected()
        {
            var packageJson = GetSelectedPackagesJson();
            EditorCoroutineUtility.StartCoroutineOwnerless(PublishAll(packageJson));
        }

        [MenuItem(PatchAndPublishAllSelectedMenu, true)]
        public static bool CanPatchAndPublishAllSelected() => !string.IsNullOrEmpty(NpmPublishWindow.Registry);

        [MenuItem(PatchAndPublishAllSelectedMenu, priority = 2000)]
        public static void PatchAndPublishAllSelected()
        {
            var packageJson = GetSelectedPackagesJson();
            EditorCoroutineUtility.StartCoroutineOwnerless(PatchAndPublish(packageJson));
        }
        */

        [MenuItem(PublishModifiedMenu, true)]
        public static bool CanPublishModifiedMenu() => !string.IsNullOrEmpty(NpmPublishWindow.Registry);

        [MenuItem(PublishModifiedMenu, priority = 2100)]
        public static void PublishModified()
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(PublishModifiedRoutine());
        }

        public static Dictionary<TextAsset, Package> GetAllLocalPackages()
        {
            var toPublish = new Dictionary<TextAsset, Package>();

            try
            {
                var locals = UpmClientUtils.FindLocalPackages();
                foreach (var localAsset in locals)
                {
                    var local = JsonUtility.FromJson<Package>(localAsset.text);
                    toPublish.Add(localAsset, local);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return toPublish;
        }

        public static Dictionary<TextAsset, Package> GetModifiedPackages()
        {
            var toPublish = new Dictionary<TextAsset, Package>();
            var localPackages = GetAllLocalPackages();

            try
            {
                var search = Client.SearchAll();

                int counter = 0;
                while (!search.IsCompleted)
                {
                    counter++;
                    var label = counter % 3 == 0 ? "Collecting info."
                        : counter % 3 == 1 ? "Collecting info.."
                        : "Collecting info...";
                    EditorUtility.DisplayProgressBar("NPM Publish", label, 1f);
                }

                foreach (var localAsset in localPackages)
                {
                    var package = localAsset.Value;
                    var searched = search.Result.FirstOrDefault(o => o.name == package.name);
                    if (searched == null || searched.versions.latest == package.version)
                        continue;
                    toPublish.Add(localAsset.Key, package);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return toPublish;
        }

        public static IEnumerable FetchModifiedPackagesRoutine(string registry,
            Dictionary<TextAsset, Package> resultPackages)
        {
            if (!registry.EndsWith("/")) registry += "/";

            var header = $"Npm: {registry}";
            var localPackages = GetAllLocalPackages();

            try
            {
                int progressIndex = 0;
                foreach (var localAsset in localPackages)
                {
                    var package = localAsset.Value;

                    EditorUtility.DisplayProgressBar(header, package.name,
                        1f * ++progressIndex / localPackages.Count);

                    var request = UnityWebRequest.Get(registry + package.name);
                    request.SendWebRequest();
                    while (!request.isDone)
                    {
                        yield return null;
                    }

                    try
                    {
                        if (request.responseCode == 200 &&
                            request.downloadHandler.text is string responseText &&
                            Json.Deserialize(responseText) is Dictionary<string, object> json &&
                            json.TryGetValue("dist-tags", out var distTagsObject) &&
                            distTagsObject is Dictionary<string, object> distTags &&
                            distTags.TryGetValue("latest", out var latestObject) &&
                            latestObject is string latest &&
                            latest != package.version)
                        {
                            resultPackages.Add(localAsset.Key, package);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to fetch {request.url}");
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static IEnumerator PublishModifiedRoutine()
        {
            var toPublish = new Dictionary<TextAsset, Package>();
            var registry = NpmPublishPreferences.Registry;

            foreach (var entry in FetchModifiedPackagesRoutine(registry, toPublish))
            {
                yield return entry;
            }

            if (toPublish.Count == 0)
            {
                var title = $"Npm: {registry}";
                EditorUtility.DisplayDialog(title, "No modified packages found", "Close");
                yield break;
            }

            yield return PublishPackages(toPublish);
        }

        public static IEnumerator PublishPackages(IDictionary<TextAsset, Package> toPublish)
        {
            var nl = Environment.NewLine;
            var message = $"Following packages would be published:" +
                          toPublish.Aggregate("", (s, c) => s + $"{nl} - {c.Value.name}: {c.Value.version}");

            var header = $"Npm {NpmPublishPreferences.Registry}";
            if (!EditorUtility.DisplayDialog(header, message, "Publish", "Cancel"))
            {
                yield break;
            }

            try
            {
                foreach (var asset in toPublish)
                {
                    EditorUtility.DisplayProgressBar("Npm Publish", asset.Key.name, 1f);

                    while (NpmUtils.IsNpmRunning)
                    {
                        yield return null;
                    }

                    bool done = false;
                    NpmPublishCommand.Execute(asset.Key, () => done = true);

                    while (!done)
                    {
                        yield return null;
                    }

                    Debug.Log("OK " + asset.Key);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /*
        public static IEnumerator PublishAll(List<TextAsset> assets)
        {
            foreach (var asset in assets)
            {
                while (NpmUtils.IsNpmRunning)
                {
                    yield return null;
                }

                NpmCommands.SetWorkingDirectory(asset);
                NpmCommands.Publish((code, msg) =>
                        Debug.Log($"NPM package {asset.name} published with {code} msg {msg}"),
                    NpmPublishWindow.Registry);
            }
        }

        public static IEnumerator PatchAndPublish(List<TextAsset> assets)
        {
            foreach (var asset in assets)
            {
                while (NpmUtils.IsNpmRunning)
                {
                    yield return null;
                }

                NpmCommands.SetWorkingDirectory(asset);
                NpmCommands.UpdateVersion(asset, NpmVersion.Patch);
                NpmCommands.Publish((code, msg) =>
                        Debug.Log($"NPM package {asset.name} published with {code} msg {msg}"),
                    NpmPublishWindow.Registry);
            }
        }
        */


        public static TextAsset GetSelectedPackageJson()
        {
            var selected = Selection.activeObject;
            return GetPackageJson(selected);
        }

        public static TextAsset GetPackageJson(Object obj)
        {
            if (obj == null) return null;

            var path = AssetDatabase.GetAssetPath(obj);
            return GetPackageJson(path);
        }
        /*
        public static List<TextAsset> GetSelectedPackagesJson()
        {
            var result = new List<TextAsset>();
            var selectedAssets = Selection.objects;
            foreach (var asset in selectedAssets)
            {
                var textAsset = GetPackageJson(asset);
                if (textAsset != null && result.Contains(textAsset) == false)
                    result.Add(textAsset);
            }

            return result;
        }
        */

        public static TextAsset GetPackageJson(string path)
        {
            if (path == null) return null;
            if (!path.StartsWith("Assets/")) return null;

            TextAsset packageAsset;
            if (path.EndsWith("/package.json") &&
                (packageAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path)) != null)
            {
                return packageAsset;
            }

            if (!AssetDatabase.IsValidFolder(path)) return null;

            foreach (var suffix in PackageJsonPaths)
            {
                var packageJsonPath = path + suffix;
                packageAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(packageJsonPath);
                if (packageAsset != null)
                {
                    return packageAsset;
                }
            }

            return null;
        }
    }
}