using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace NpmPackageLoader.Loaders
{
#if PUBLISHER_ENV
    [CreateAssetMenu(fileName = "Ftp UnityPackage Loader", menuName = "NPM/Ftp UnityPackage Loader")]
#endif
    public class FtpUnityPackageLoader : UnityPackageLoader
    {
        [SerializeField] private string url = default;
        [SerializeField] private string user = default;
        [SerializeField] private string readPassword = default;
        [SerializeField] private string writePassword = default;

#if UNITY_EDITOR
        private string GetTempUnityPackagePath()
        {
            var unityPackageFile = Application.dataPath;
            unityPackageFile = unityPackageFile.Remove(unityPackageFile.Length - "Assets/".Length);
            unityPackageFile = unityPackageFile + "/Temp/" + name + ".unitypackage";
            return unityPackageFile;
        }

        public override void Import(TextAsset packageJsonAsset, Action success, Action fail)
        {
            ExecuteAction(() =>
            {
                var packageJson = JsonUtility.FromJson<PackageJson>(packageJsonAsset.text);
                var unityPackageFile = GetTempUnityPackagePath();

                var data = new FtpData
                {
                    Url = url,
                    User = user,
                    Password = readPassword,
                    LocalFile = unityPackageFile,
                    PackageName = packageJson.name,
                    PackageVersion = packageJson.version,
                    UnityPackageName = name,
                };

                FtpUtils.Download(data, () =>
                {
                    AssetDatabase.ImportPackage(unityPackageFile, true);
                    success();
                }, fail);
            }, fail);
        }

        public override void Export(TextAsset packageJsonAsset, Action success, Action fail)
        {
            var writePass = writePassword;
            writePassword = string.Empty;
            EditorUtility.SetDirty(this);
            AssetDatabase.Refresh();

            ExecuteAction(() =>
            {
                var packageJson = JsonUtility.FromJson<PackageJson>(packageJsonAsset.text);
                var unityPackageFile = GetTempUnityPackagePath();

                ExportUnityPackage(packageJsonAsset, unityPackageFile, () =>
                {
                    writePassword = writePass;
                    EditorUtility.SetDirty(this);
                    AssetDatabase.Refresh();

                    var data = new FtpData
                    {
                        Url = url,
                        User = user,
                        Password = writePassword,
                        LocalFile = unityPackageFile,
                        PackageName = packageJson.name,
                        PackageVersion = packageJson.version,
                        UnityPackageName = name,
                    };

                    FtpUtils.Upload(data, success, fail);
                }, fail);
            }, fail);
        }
#endif
    }
}