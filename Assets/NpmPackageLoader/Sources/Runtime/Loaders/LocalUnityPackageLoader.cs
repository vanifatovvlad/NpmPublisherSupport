using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace NpmPackageLoader.Loaders
{
#if PUBLISHER_ENV
    [CreateAssetMenu(fileName = "Local UnityPackage Loader", menuName = "NPM/Local UnityPackage Loader")]
#endif
    public class LocalUnityPackageLoader : UnityPackageLoader
    {
#if UNITY_EDITOR
        private string GetUnityPackagePath()
        {
            var loaderPath = AssetDatabase.GetAssetPath(this);
            var loaderDirectory = Path.GetDirectoryName(loaderPath) ?? string.Empty;
            var unityPackagePath = Path.Combine(loaderDirectory, name + "-UnityPackage.unitypackage");
            return unityPackagePath;
        }

        public override void Import(TextAsset packageJsonAsset, Action success, Action fail)
        {
            Try(fail, () =>
            {
                var unityPackagePath = GetUnityPackagePath();
                AssetDatabase.ImportPackage(unityPackagePath, true);
                success();
            });
        }

        public override void Export(TextAsset packageJsonAsset, Action success, Action fail)
        {
            Try(fail, () =>
            {
                var unityPackagePath = GetUnityPackagePath();

                Export(packageJsonAsset, unityPackagePath, () =>
                {
                    AssetDatabase.ImportAsset(unityPackagePath);
                    success();
                }, fail);
            });
        }

#endif
    }
}