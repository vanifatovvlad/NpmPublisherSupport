using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NpmPackageLoader.Sources.Editor
{
    public static class PackageDatabase
    {
        public const string PackagesFolder = "Packages/";

        private static List<PackageUpdateInfo> _packages;

        public static List<PackageUpdateInfo> Packages
        {
            get
            {
                if (_packages != null) return _packages;
                RefreshPackagesToUpdate();
                return _packages;
            }
        }

        public static IEnumerable<PackageUpdateInfo> NotInstalledPackages
            => Packages.Where(p => p.IsNotInstalled);

        public static IEnumerable<PackageUpdateInfo> UpdateReadyPackages
            => Packages.Where(p => p.IsReadyForUpdate);

        public static IEnumerable<PackageUpdateInfo> UpToDatePackages
            => Packages.Where(p => p.IsUpToDate);

        public static void RefreshPackagesToUpdate()
        {
            _packages = new List<PackageUpdateInfo>();

            var assetPaths = GetLoaderPaths();

            foreach (var loaderPath in assetPaths)
            {
                var loaderName = Path.GetFileNameWithoutExtension(loaderPath);
                var loaderDirectory = Path.GetDirectoryName(loaderPath);
                if (loaderDirectory == null)
                    continue;

                var packageJsonPath = Path.Combine(loaderDirectory, "package.json");
                var packageJsonContent = File.ReadAllText(packageJsonPath);
                var packageJson = JsonUtility.FromJson<PackageJson>(packageJsonContent);

                var embedPackageJsonPath = $"{Application.dataPath}/Packages/{packageJson.name}/{loaderName}.json";

                PackageJson embedPackageJson = null;
                if (File.Exists(embedPackageJsonPath))
                {
                    var embedPackageJsonContent = File.ReadAllText(embedPackageJsonPath);
                    embedPackageJson = JsonUtility.FromJson<PackageJson>(embedPackageJsonContent);
                }

                var info = new PackageUpdateInfo
                {
                    Package = packageJson,
                    InstalledVersion = embedPackageJson?.version,
                    AvailableVersion = packageJson.version,
                    PackageJsonPath = packageJsonPath,
                    LoaderPath = loaderPath,
                    LoaderName = loaderName,
                };
                _packages.Add(info);
            }
        }

        public static List<string> GetLoaderPaths()
        {
            return AssetDatabase.FindAssets($"t:{typeof(Loader).FullName}", new[] {"Packages"})
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.StartsWith(PackagesFolder))
                .ToList();
        }

        public static void InstallOrUpdate(PackageUpdateInfo info)
        {
            var installOrUpdate = info.IsNotInstalled ? "Install" : "Update";
            var message = $"'{info.Package.name}:{info.Package.version}' " +
                          $"require download additional unityPackage '{info.LoaderName}'";

            if (!EditorUtility.DisplayDialog("Npm Package Loader", message, installOrUpdate, "Cancel"))
            {
                return;
            }

            var packageJsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(info.PackageJsonPath);
            var loader = AssetDatabase.LoadAssetAtPath<Loader>(info.LoaderPath);

            void OnSuccess()
            {
                Debug.Log($"{info.Package.name}:{info.LoaderName} updated");
            }

            void OnFail()
            {
                Debug.LogError($"Failed to update {info.Package.name}:{info.LoaderName}");
            }

            loader.Import(packageJsonAsset, OnSuccess, OnFail);
        }
    }
}