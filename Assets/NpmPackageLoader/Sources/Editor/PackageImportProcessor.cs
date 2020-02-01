using NpmPackageLoader.Sources.Editor;
using UnityEditor;

namespace NpmPackageLoader
{
    internal class PackageImportProcessor : AssetPostprocessor
    {
        private static bool Checked
        {
            get => SessionState.GetBool("npm-loader.checked", false);
            set => SessionState.SetBool("npm-loader.checked", value);
        }

        [InitializeOnLoadMethod]
        private static void Setup()
        {
            AssetDatabase.importPackageCancelled += OnImportPackageCancelled;
            AssetDatabase.importPackageFailed += OnImportPackageFailed;
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;

            if (Checked) return;

            Checked = true;
            CheckDelayed();
        }

        private static void OnImportPackageCompleted(string packageName) => CheckDelayed();
        private static void OnImportPackageFailed(string packageName, string errormessage) => CheckDelayed();
        private static void OnImportPackageCancelled(string packageName) => CheckDelayed();

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var importedAsset in importedAssets)
            {
                if (importedAsset.StartsWith(PackageDatabase.PackagesFolder))
                {
                    CheckDelayed();
                    return;
                }
            }
        }

        private static bool _checkScheduled;

        private static void CheckDelayed()
        {
            if (_checkScheduled) return;
            _checkScheduled = true;

            EditorApplication.delayCall += Check;
        }

        private static void Check()
        {
            _checkScheduled = false;

            PackageDatabase.RefreshPackagesToUpdate();

            if (PackageDatabase.Packages.Count > 0)
            {
                PackagesWindow.ShowPackagesWindow();
            }
        }
    }
}