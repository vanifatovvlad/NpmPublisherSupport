using System;
using System.IO;
using System.Linq;
using NpmPackageLoader;
using UnityEditor;
using UnityEngine;

namespace NpmPublisherSupport
{
    public static class NpmPublishCommand
    {
        public static void Execute(TextAsset packageJsonAsset, Action callback)
        {
            Execute(packageJsonAsset, callback, new NpmPublishCommandOptions
            {
                CopyDocumentation = true,
                CopySamples = true,
            });
        }

        public static void Execute(TextAsset packageJsonAsset, Action callback,
            NpmPublishCommandOptions options)
        {
            NpmCommands.SetWorkingDirectory(packageJsonAsset);

            if (options.CopyDocumentation)
            {
                CopyDirectoryIfExists(packageJsonAsset, "Documentation", "Documentation~");
            }

            if (options.CopySamples)
            {
                CopyDirectoryIfExists(packageJsonAsset, "Samples", "Samples~");
            }

            var packageJsonPath = AssetDatabase.GetAssetPath(packageJsonAsset);
            var packageExternalLoaders = AssetDatabase
                .FindAssets($"t:{typeof(Loader).FullName}", new[] {Path.GetDirectoryName(packageJsonPath)})
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Loader>)
                .ToList();

            void Step(int index)
            {
                if (index == packageExternalLoaders.Count)
                {
                    NpmCommands.SetWorkingDirectory(packageJsonAsset);
                    NpmCommands.Publish((code, result) => callback(), NpmPublishWindow.Registry);
                }
                else
                {
                    var loader = packageExternalLoaders[index];

                    loader.Export(packageJsonAsset,
                        () => Step(index + 1),
                        () => Debug.LogError($"Failed to export {loader.name}"));
                }
            }

            Step(0);
        }

        private static void CopyDirectoryIfExists(TextAsset packageJsonAsset, string src, string dst)
        {
            var packageDirectory = NpmCommands.GetPackageDirectory(packageJsonAsset);

            var documentationSrcPath = packageDirectory + src;
            var documentationDstPath = packageDirectory + dst;

            if (Directory.Exists(documentationSrcPath))
            {
                FileUtil.ReplaceDirectory(documentationSrcPath, documentationDstPath);
            }
        }
    }

    public struct NpmPublishCommandOptions
    {
        public bool CopySamples;
        public bool CopyDocumentation;
    }
}