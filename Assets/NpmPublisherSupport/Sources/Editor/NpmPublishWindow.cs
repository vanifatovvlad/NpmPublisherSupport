using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NpmPackageLoader;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace NpmPublisherSupport
{
    public class NpmPublishWindow : EditorWindow, IHasCustomMenu
    {
        private static readonly GUIContent PatchDependentContent = new GUIContent("Patch Dependent",
            "If on, version of dependant packages in this project will be automatically Patched");

        public static void OpenPublish(TextAsset packageJson)
        {
            var window = GetWindow<NpmPublishWindow>();
            window.titleContent = new GUIContent("Npm Publish");
            window.minSize = new Vector2(500, 350);
            window.packageAsset = packageJson;
            window.RefreshImmediate(false);
            window.Show();
        }

        internal static string Registry
        {
            get => EditorPrefs.GetString("codewriter.npm-publisher-support.registry", "");
            set => EditorPrefs.SetString("codewriter.npm-publisher-support.registry", value);
        }

        internal static bool UpdateVersionRecursively
        {
            get => EditorPrefs.GetInt("codewriter.npm-publisher-support.update-recursively", 1) == 1;
            set => EditorPrefs.SetInt("codewriter.npm-publisher-support.update-recursively", value ? 1 : 0);
        }

        [SerializeField] private TextAsset packageAsset;
        [SerializeField] private bool userFetched;
        [SerializeField] private string user = "";
        [SerializeField] private string directory = "";
        [SerializeField] private Package package = new Package();
        [SerializeField] private string[] packageJsonLines = new string[0];
        [SerializeField] private List<Loader> packageExternalLoaders = new List<Loader>();

        [SerializeField] private string registryInput = "";
        [SerializeField] private Vector2 packageJsonScroll;

        private void Refresh(bool force) => EditorApplication.delayCall += () => RefreshImmediate(force);

        private void RefreshImmediate(bool force)
        {
            var path = AssetDatabase.GetAssetPath(packageAsset);
            if (force)
            {
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(path);
            }

            packageAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            directory = string.Empty;
            package = JsonUtility.FromJson<Package>(packageAsset.text);

            var packageJsonObj = MiniJSON.Json.Deserialize(packageAsset.text);
            var packageJsonFormatted = MiniJSON.Json.Serialize(packageJsonObj);
            packageJsonLines = packageJsonFormatted.Split(new[] {Environment.NewLine}, StringSplitOptions.None);

            packageExternalLoaders = AssetDatabase
                .FindAssets($"t:{typeof(Loader).FullName}", new[] {Path.GetDirectoryName(path)})
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Loader>)
                .ToList();

            Repaint();
        }

        private void FetchUser()
        {
            user = string.Empty;
            userFetched = true;
            NpmUtils.ExecuteNpmCommand($"whoami --registry {Registry}", (code, result) =>
            {
                if (code == 0)
                {
                    user = result.Trim();
                }

                Refresh(false);
            });
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            NpmPublishAssetProcessor.PackageImported += PackageImported;

            if (!UpmClientUtils.IsListed)
            {
                UpmClientUtils.ListPackages(() => Refresh(false));
            }
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            NpmPublishAssetProcessor.PackageImported -= PackageImported;
        }

        private void OnSelectionChanged()
        {
            var newPackageJson = NpmPublishMenu.GetSelectedPackageJson();
            if (newPackageJson != null && newPackageJson != packageAsset)
            {
                packageAsset = newPackageJson;
                RefreshImmediate(false);
            }
        }

        private void PackageImported()
        {
            UpmClientUtils.ListLocalPackages();
            Refresh(false);
        }

        private void OnGUI()
        {
            GUI.enabled = !NpmUtils.IsNpmRunning && !UpmClientUtils.IsUpmRunning;

            if (string.IsNullOrEmpty(Registry))
            {
                DrawNoRegistry();
                return;
            }

            if (string.IsNullOrEmpty(directory))
            {
                directory = NpmCommands.GetPackageDirectory(packageAsset);
            }

            NpmUtils.WorkingDirectory = directory;

            EditorGUI.BeginChangeCheck();
            DrawToolbar();
            if (EditorGUI.EndChangeCheck())
            {
                return;
            }

            if (!userFetched)
            {
                FetchUser();
            }

            if (string.IsNullOrEmpty(user))
            {
                DrawNotLoggedIn();
                return;
            }

            DrawContent();

            GUILayout.FlexibleSpace();

            GUI.enabled = true;
        }

        private void DrawContent()
        {
            GUILayout.Space(10);
            DrawContentPackageJson();
            GUILayout.Space(10);

            DrawContentPackageInfo();
            GUILayout.Space(10);

            DrawPackageExternalLoaders();
            GUILayout.Space(10);

            if (GUILayout.Button("Publish", GUILayout.Height(24)))
            {
                DoPublish();
            }
        }

        private void DoPublish()
        {
            var msg = $"Are you really want to publish package {package.name}: {package.version}?";
            if (!EditorUtility.DisplayDialog("Npm", msg, "Publish", "Cancel")) return;

            NpmPublishCommand.Execute(packageAsset, () =>
            {
                //
                UpmClientUtils.ListPackages(() => Refresh(false));
            });
        }

        private void DrawContentPackageJson()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(package.displayName, Styles.HeaderDisplayNameLabel);
            GUILayout.Label(package.version, Styles.HeaderVersionLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Check updates"))
            {
                UpmClientUtils.ListPackages(() => Refresh(false));
            }

            GUILayout.EndHorizontal();
            GUILayout.Label(package.name, Styles.HeaderNameLabel);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Package.json", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open", EditorStyles.miniButton))
            {
                AssetDatabase.OpenAsset(packageAsset);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            packageJsonScroll = GUILayout.BeginScrollView(packageJsonScroll, Styles.BigTitle);

            bool dependenciesBlock = false;
            foreach (var line in packageJsonLines)
            {
                if (line.Contains("}"))
                    dependenciesBlock = false;

                if (dependenciesBlock &&
                    ExtractPackageInfoFromJsonLine(line, out string packageName, out string packageVersion))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(line, Styles.RichNoPaddingLabel);
                    GUILayout.Space(5);
                    DrawDependencyQuickActions(packageName, packageVersion);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label(line, Styles.RichNoPaddingLabel);
                }

                if (line.Contains("\"dependencies\"") && line.Contains("{") && !line.Contains("}"))
                    dependenciesBlock = true;
            }

            GUILayout.EndScrollView();
        }

        private void DrawDependencyQuickActions(string packageName, string packageVersion)
        {
            var oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 0.5f);

            try
            {
                var latestVersion = UpmClientUtils.GetPackageVersion(packageName, PackageVersionType.UpmLatest);
                if (latestVersion == "") // package not exist in UPM
                {
                    var localVersion = UpmClientUtils.GetPackageVersion(packageName, PackageVersionType.Local);
                    if (localVersion == "") // package not exists Locally
                    {
                        GUILayout.Label("<color=#960012>Unknown</color>", Styles.RichNoPaddingLabel);
                        return;
                    }

                    if (localVersion == packageVersion) // package up to date with local
                    {
                        GUILayout.Label("<color=#00000055>LOCAL: Up to date</color>", Styles.RichNoPaddingLabel);
                        return;
                    }

                    // package can be updated to local version
                    if (GUILayout.Button($"LOCAL: Update to {localVersion}", EditorStyles.miniButton))
                    {
                        NpmCommands.SetDependencyVersion(packageAsset, packageName, localVersion);
                        RefreshImmediate(true);
                    }

                    return;
                }

                if (latestVersion == packageVersion) // package up to date with upm(remote)
                {
                    GUILayout.Label("<color=#00000055>Up to date</color>", Styles.RichNoPaddingLabel);
                    return;
                }

                // package can be updated to upm(remote) version
                if (GUILayout.Button($"Update to {latestVersion}", EditorStyles.miniButton))
                {
                    var msg = $"Are you really want to install package\n{packageName}: {latestVersion}";
                    if (EditorUtility.DisplayDialog("Package Manager", msg, "Install", "Cancel"))
                    {
                        NpmCommands.SetDependencyVersion(packageAsset, packageName, latestVersion);
                        RefreshImmediate(true);
                        Client.Add($"{packageName}@{latestVersion}");
                    }
                }
            }
            finally
            {
                GUI.color = oldColor;
            }
        }

        private void DrawContentPackageInfo()
        {
            EditorGUILayout.LabelField("Name", package.name);
            EditorGUILayout.LabelField("Version", package.version);

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Increment Version");

                var options = new[] {GUILayout.MaxWidth(110)};

                if (GUILayout.Button("Major 1.0.0", EditorStyles.miniButtonLeft, options))
                    UpdateVersion(NpmVersion.Major);

                if (GUILayout.Button("Minor 0.1.0", EditorStyles.miniButtonMid, options))
                    UpdateVersion(NpmVersion.Minor);

                if (GUILayout.Button("Patch 0.0.1", EditorStyles.miniButtonRight, options))
                    UpdateVersion(NpmVersion.Patch);

                GUILayout.Space(5);

                UpdateVersionRecursively =
                    GUILayout.Toggle(UpdateVersionRecursively, PatchDependentContent, EditorStyles.toggle);

                GUILayout.FlexibleSpace();
            }

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Directory");
            EditorGUILayout.TextArea(directory, EditorStyles.wordWrappedLabel);
            GUILayout.EndHorizontal();
        }

        private void DrawPackageExternalLoaders()
        {
            const string NpmPackageLoader = "com.greenbuttongames.npm-package-loader";

            if (packageExternalLoaders.Count == 0)
                return;

            GUILayout.Label("External loaders", EditorStyles.boldLabel);

            var loaderPackageVersion = NpmCommands.GetDependencyVersion(packageAsset, NpmPackageLoader);

            if (loaderPackageVersion == null)
            {
                var version = UpmClientUtils.GetPackageVersion(NpmPackageLoader, PackageVersionType.UpmLatest);

                if (string.IsNullOrEmpty(version))
                {
                    version = UpmClientUtils.GetPackageVersion(NpmPackageLoader, PackageVersionType.Local);
                }

                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"Missing {NpmPackageLoader}:{version} dependency", MessageType.Error);
                if (GUILayout.Button("Add", GUILayout.Height(40)))
                {
                    NpmCommands.SetDependencyVersion(packageAsset, NpmPackageLoader, version);
                    Refresh(true);
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }

            GUILayout.BeginVertical(Styles.BigTitle);
            foreach (var loader in packageExternalLoaders)
            {
                EditorGUILayout.ObjectField(loader, typeof(Loader), false);
            }

            //     "com.greenbuttongames.npm-package-loader": "0.1.9"


            GUILayout.EndVertical();
        }

        private void UpdateVersion(NpmVersion version)
        {
            if (UpdateVersionRecursively)
            {
                NpmCommands.UpdateVersionRecursively(packageAsset, version);
            }
            else
            {
                NpmCommands.UpdateVersion(packageAsset, version);
            }

            UpmClientUtils.ListLocalPackages();
            Refresh(true);
        }

        private static bool ExtractPackageInfoFromJsonLine(string line, out string packageName,
            out string packageVersion)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                packageName = null;
                packageVersion = null;
                return false;
            }

            try
            {
                line = line.Substring(line.IndexOf("\"", StringComparison.Ordinal) + 1);
                packageName = line.Substring(0, line.IndexOf("\"", StringComparison.Ordinal)).Trim();

                line = line.Substring(line.IndexOf("\"", StringComparison.Ordinal) + 1);
                line = line.Substring(line.IndexOf("\"", StringComparison.Ordinal) + 1);
                packageVersion = line.Substring(0, line.IndexOf("\"", StringComparison.Ordinal)).Trim();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                packageName = null;
                packageVersion = null;
                return false;
            }
        }

        private void DrawToolbar()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Registry", EditorStyles.toolbarButton);
                GUILayout.Label(Registry, EditorStyles.toolbarButton);
                if (GUILayout.Button("Edit", EditorStyles.toolbarButton))
                {
                    Registry = string.Empty;
                    userFetched = false;
                    user = string.Empty;
                    RefreshImmediate(false);
                    return;
                }

                GUILayout.FlexibleSpace();

                if (!string.IsNullOrEmpty(user))
                {
                    GUILayout.Label("User", EditorStyles.toolbarButton);
                    GUILayout.Label(user, EditorStyles.toolbarButton);
                }
            }
        }

        private void DrawNoRegistry()
        {
            CenteredMessage(() =>
            {
                GUILayout.Label("No registry selected", Styles.CenteredLargeLabel);
                GUILayout.Space(10);
                GUILayout.Label("Registry:");
                registryInput = GUILayout.TextField(registryInput, GUILayout.MaxWidth(600)).Trim();
                GUILayout.Space(10);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Confirm", GUILayout.Width(180), GUILayout.Height(24)))
                    {
                        Registry = registryInput;
                        registryInput = string.Empty;
                    }

                    GUILayout.FlexibleSpace();
                }
            });
        }

        private void DrawNotLoggedIn()
        {
            CenteredMessage(() =>
            {
                if (NpmUtils.IsNpmRunning)
                {
                    GUILayout.Label("Logging in...", Styles.CenteredLargeLabel);
                    return;
                }

                GUILayout.Label("Not logged in", Styles.CenteredLargeLabel);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("1. Download and install");
                    if (GUILayout.Button("Node JS", EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        Application.OpenURL("https://nodejs.org");
                    }

                    GUILayout.FlexibleSpace();
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("2. Open terminal and run");
                    GUILayout.TextField($"npm adduser --registry {Registry}");
                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(10);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Refresh", GUILayout.Width(180), GUILayout.Height(24)))
                    {
                        userFetched = false;
                    }

                    GUILayout.FlexibleSpace();
                }
            });
        }

        private static void CenteredMessage(Action action, int space = 50)
        {
            using (new GUILayout.VerticalScope())
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.HorizontalScope(Styles.BigTitleWithPadding))
                {
                    GUILayout.Space(space);
                    using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                    {
                        action();
                    }

                    GUILayout.Space(space);
                }

                GUILayout.FlexibleSpace();
            }
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Change Registry"), false, Logout);
        }

        private void Logout()
        {
            Registry = string.Empty;
        }
    }
}