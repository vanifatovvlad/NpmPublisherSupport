using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NpmPublisherSupport
{
    internal static class NpmPublishMenu
    {
        private const string PublishMenuItemPath = "Assets/Publish Npm Package";
        private const string PublishAllSelectedMenu = "Assets/NPM/Publish ALL Selected";
        private const string PatchAndPublishAllSelectedMenu = "Assets/NPM/Publish and Patch ALL Selected";
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

        [MenuItem(PublishAllSelectedMenu, priority = 2000)]
        public static void PublishAllSelected()
        {
            var packageJson = GetSelectedPackagesJson();
            EditorCoroutineUtility.StartCoroutineOwnerless(PublishAll(packageJson));
        }

        [MenuItem(PatchAndPublishAllSelectedMenu, priority = 2000)]
        public static void PatchAndPublishAllSelected()
        {
            var packageJson = GetSelectedPackagesJson();
            EditorCoroutineUtility.StartCoroutineOwnerless(PatchAndPublish(packageJson));
        }

        [MenuItem(PublishModifiedMenu, priority = 2100)]
        public static void PublishModified()
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(PublishModifiedRoutine());
        }

        public static IEnumerator PublishModifiedRoutine()
        {
            var toPublish = new Dictionary<TextAsset, Package>();

            try
            {
                var search = Client.SearchAll();

                int counter = 0;
                while (!search.IsCompleted)
                {
                    counter++;
                    var label = counter % 3 == 0 ? "Collecting info."
                        : counter % 3 == 0 ? "Collecting info.."
                        : "Collecting info...";
                    EditorUtility.DisplayProgressBar("NPM Publish", label, 1f);
                    yield return null;
                }

                var locals = UpmClientUtils.FindLocalPackages();

                foreach (var localAsset in locals)
                {
                    var local = JsonUtility.FromJson<Package>(localAsset.text);
                    var searched = search.Result.FirstOrDefault(o => o.name == local.name);

                    if (searched == null || searched.versions.latest == local.version)
                        continue;

                    toPublish.Add(localAsset, local);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            var message = $"Following packages would be published:" +
                          toPublish.Aggregate("", (s, c) => s + $"{Environment.NewLine} - {c.Value.name}");

            if (EditorUtility.DisplayDialog("Publish?", message, "Publish", "Cancel"))
            {
                foreach (var asset in toPublish)
                {
                    while (NpmUtils.IsNpmRunning)
                    {
                        yield return null;
                    }

                    NpmCommands.SetWorkingDirectory(asset.Key);
                    NpmCommands.Publish((code, msg) =>
                            Debug.Log($"NPM package {asset.Value.name} published with {code} msg {msg}"),
                        NpmPublishWindow.Registry);
                }
            }
        }

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