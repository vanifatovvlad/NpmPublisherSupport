using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NpmPackageLoader
{
    internal class PackageImportProcessor : AssetPostprocessor
    {
        private const string PackagesFolder = "Packages/";

        private static bool Checked
        {
            get => SessionState.GetBool("npm-loader.checked", false);
            set => SessionState.SetBool("npm-loader.checked", value);
        }

        [InitializeOnLoadMethod]
        private static void Setup()
        {
            if (Checked) return;

            Checked = true;
            CheckDelayed();
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var importedAsset in importedAssets)
            {
                if (importedAsset.StartsWith(PackagesFolder))
                {
                    CheckDelayed();
                    return;
                }
            }
        }

        private static void CheckDelayed() => EditorApplication.delayCall += Check;

        [MenuItem("Window/Check Npm Package Loaders")]
        private static void Check()
        {
            var assetPaths = AssetDatabase.FindAssets($"t:{typeof(Loader).FullName}", new[] {"Packages"})
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.StartsWith(PackagesFolder))
                .ToList();

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

                if (embedPackageJson != null && embedPackageJson.version == packageJson.version)
                    continue;

                var installOrUpdate = embedPackageJson == null ? "Install" : "Update";
                var message = $"'{packageJson.name}:{packageJson.version}' " +
                              $"require download additional unitypackage '{loaderName}'";

                if (!EditorUtility.DisplayDialog("Npm Package Loader", message, installOrUpdate, "Cancel"))
                    continue;

                Checked = false;

                var packageJsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(packageJsonPath);
                var loader = AssetDatabase.LoadAssetAtPath<Loader>(loaderPath);

                loader.Import(packageJsonAsset,
                    () => Debug.Log("Success"),
                    () => Debug.LogError("fail")
                );

                return;
            }

            Checked = true;
        }
    }
}