using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NpmPublisherSupport
{
    public enum NpmVersion : byte
    {
        Major,
        Minor,
        Patch,
    }
    
    public class NpmCommands
    {

        public static void Publish(NpmCommandCallback action, string registry = "")
        {
            if (string.IsNullOrEmpty(registry))
            {
                NpmUtils.ExecuteNpmCommand($"publish ", action);
            }
            else
            {
                NpmUtils.ExecuteNpmCommand($"publish --registry {registry}", action);
            }         
        }

        public static void UpdateVersion(NpmCommandCallback action,NpmVersion version)
        {
            
            switch (version)
            {
                case NpmVersion.Major:
                    NpmUtils.ExecuteNpmCommand($"version major", action);
                    break;
                case NpmVersion.Minor:
                    NpmUtils.ExecuteNpmCommand($"version minor", action);
                    break;
                case NpmVersion.Patch:
                    NpmUtils.ExecuteNpmCommand($"version patch", action);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(version), version, null);
            }
        }


        public static string GetPackageDirectory(TextAsset package)
        {
            var assetPath = AssetDatabase.GetAssetPath(package);
            var path = Application.dataPath;
            path = path.Substring(0, path.Length - "Assets".Length);
            path = path + AssetDatabase.GetAssetPath(package);
            var fileName = Path.GetFileName(assetPath);
            path = path.Substring(0, path.Length - fileName.Length);
            var directory = Path.GetFullPath(path);
            return directory;
        }

        public static void SetWorkingDirectory(TextAsset package)
        {
            var directory = GetPackageDirectory(package);
            NpmUtils.WorkingDirectory = directory;
        }

        public static void PublishAllPackages(List<TextAsset> package)
        {
            foreach (var asset in package)
            {
                SetWorkingDirectory(asset);
                Publish((x,y) => Debug.Log($"PUBLISH {asset.name} CODE: {x} MSG: {y}"));             
            }
        }
    }
    
}