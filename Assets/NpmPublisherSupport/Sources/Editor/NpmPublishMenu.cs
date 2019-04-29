using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NpmPublisherSupport
{
    internal static class NpmPublishMenu
    {
        private const string PublishMenuItemPath = "Assets/Publish Npm Package";
        private const string PublishAllSelectedMenu = "Assets/NPM/Publish ALL Selected";
        private const string PatchAndPublishAllSelectedMenu = "Assets/NPM/Publish and Patch ALL Selected";
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
            NpmCommands.PublishAllPackages(packageJson);
        }
        
        [MenuItem(PatchAndPublishAllSelectedMenu, priority = 2000)]
        public static void PatchAndPublishAllSelected()
        {
            var packageJson = GetSelectedPackagesJson();
            foreach (var asset in packageJson)
            {
                NpmCommands.SetWorkingDirectory(asset);
                NpmCommands.UpdateVersion((x, y) => {
                    NpmCommands.Publish((code,msg) => Debug.Log($"NPM package {asset.name} published with {code} msg {msg}"));
                },NpmVersion.Patch);
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
                if(textAsset!=null && result.Contains(textAsset) == false)
                    result.Add(textAsset);
            }

            return result;
        }

        public static TextAsset GetPackageJson(string path)
        {
            if (path == null) return null;
            if (!path.StartsWith("Assets/")) return null;
            if (!AssetDatabase.IsValidFolder(path)) return null;

            foreach (var suffix in PackageJsonPaths)
            {
                var packageJsonPath = path + suffix;
                var packageAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(packageJsonPath);
                if (packageAsset != null)
                {
                    return packageAsset;
                }
            }

            return null;
        }
    }
}