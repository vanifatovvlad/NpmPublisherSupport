using UnityEditor;
using UnityEngine;

namespace NpmPublisherSupport
{
    internal static class NpmPublishMenu
    {
        private const string PublishMenuItemPath = "Assets/Publish Npm Package";
        private static readonly string[] PackageJsonPaths = {"/package.json", "/Sources/package.json"};

        [MenuItem(PublishMenuItemPath, true)]
        public static bool CanOpenPublishWindow() => GetSelectedPackageJson() != null;

        [MenuItem(PublishMenuItemPath, priority = 2000)]
        public static void OpenPublishWindow()
        {
            var packageJson = GetSelectedPackageJson();
            NpmPublishWindow.OpenPublish(packageJson);
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