using System;
using UnityEditor;

namespace NpmPublisherSupport
{
    internal class NpmPublishAssetProcessor : AssetPostprocessor
    {
        public static Action PackageImported;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (PackageImported == null)
                return;

            foreach (string asset in importedAssets)
            {
                if (asset.StartsWith("Assets/") && asset.EndsWith("/package.json"))
                {
                    PackageImported.Invoke();
                    return;
                }
            }
        }
    }
}