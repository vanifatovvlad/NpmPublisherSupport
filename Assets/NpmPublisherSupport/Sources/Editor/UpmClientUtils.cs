using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace NpmPublisherSupport
{
    public static class UpmClientUtils
    {
        private static ListRequest _listRequest;

        public static bool IsUpmRunning => IsListing;
        public static bool IsListing => _listRequest != null && !_listRequest.IsCompleted;

        public static bool IsListed
        {
            get => SessionState.GetBool("UpmClientUtils.hasVer", false);
            private set => SessionState.SetBool("UpmClientUtils.hasVer", value);
        }

        public static void ListPackages(Action callback)
        {
            _listRequest = Client.List();
            EditorCoroutineUtility.StartCoroutineOwnerless(ListPackagesRoutine(callback));
        }

        private static IEnumerator ListPackagesRoutine(Action callback)
        {
            while (!_listRequest.IsCompleted)
            {
                yield return null;
            }

            foreach (var packageInfo in _listRequest.Result)
            {
                SetPackageVersion(packageInfo.name, PackageVersionType.UpmLatest,
                    packageInfo.versions.latestCompatible);
            }

            var localPackages = AssetDatabase.FindAssets("package t:TextAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.StartsWith("Assets/") && path.EndsWith("/package.json"))
                .Select(AssetDatabase.LoadAssetAtPath<TextAsset>)
                .ToList();
            
            foreach (var localPackage in localPackages)
            {
                try
                {
                    var package = (Dictionary<string, object>)MiniJSON.Json.Deserialize(localPackage.text);
                    var packageName = (string)package["name"];
                    var packageVersion = (string)package["version"];
                    SetPackageVersion(packageName, PackageVersionType.Local, packageVersion);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            IsListed = true;
            callback.Invoke();
        }

        public static string GetPackageVersion(string name, PackageVersionType type)
            => SessionState.GetString($"UpmClientUtils.{type}.{name}", "");

        public static void SetPackageVersion(string name, PackageVersionType type, string version)
            => SessionState.SetString($"UpmClientUtils.{type}.{name}", version);
    }

    public enum PackageVersionType
    {
        UpmLatest = 0,
        Local = 1,
    }
}