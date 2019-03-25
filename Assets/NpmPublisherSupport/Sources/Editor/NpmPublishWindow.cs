using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NpmPublisherSupport
{
    public class NpmPublishWindow : EditorWindow
    {
        public static void OpenPublish(TextAsset packageJson)
        {
            var window = GetWindow<NpmPublishWindow>(true);
            window.titleContent = new GUIContent("Npm Publish");
            window.minSize = new Vector2(500, 350);
            window.packageJson = packageJson;
            window.ShowUtility();
        }

        private string registry
        {
            get => EditorPrefs.GetString("codewriter.npm-publisher-support.registry", "");
            set => EditorPrefs.SetString("codewriter.npm-publisher-support.registry", value);
        }

        [SerializeField] private TextAsset packageJson;
        [SerializeField] private bool userFetched;
        [SerializeField] private string user = "";
        [SerializeField] private string directory = "";
        [SerializeField] private Package package = new Package();

        [SerializeField] private string registryInput = "";
        [SerializeField] private Vector2 packageJsonScroll;

        private void Refresh() => EditorApplication.delayCall += RefreshImmediate;

        private void RefreshImmediate()
        {
            var path = AssetDatabase.GetAssetPath(packageJson);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path);
            packageJson = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            directory = string.Empty;
            package = new Package();
            Repaint();
        }

        private void FetchPackageInfo()
        {
            package = JsonUtility.FromJson<Package>(packageJson.text);
        }

        private void FetchDirectory()
        {
            var path = Application.dataPath;
            path = path.Substring(0, path.Length - "Assets".Length);
            path = path + AssetDatabase.GetAssetPath(packageJson);
            var fileName = Path.GetFileName(path);
            path = path.Substring(0, path.Length - fileName.Length);
            directory = Path.GetFullPath(path);
        }

        private void FetchUser()
        {
            user = string.Empty;
            userFetched = true;
            NpmUtils.ExecuteNpmCommand($"whoami --registry {registry}", (code, result) =>
            {
                if (code == 0)
                {
                    user = result.Trim();
                }

                Refresh();
            });
        }

        private void OnGUI()
        {
            GUI.enabled = !NpmUtils.IsNpmRunning;

            if (string.IsNullOrEmpty(registry))
            {
                DrawNoRegistry();
                return;
            }

            if (string.IsNullOrEmpty(package.name))
            {
                FetchPackageInfo();
            }

            if (string.IsNullOrEmpty(directory))
            {
                FetchDirectory();
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

            GUILayout.Label("Package.json", EditorStyles.boldLabel);
            packageJsonScroll = GUILayout.BeginScrollView(packageJsonScroll, Styles.BigTitle);
            GUILayout.Label(packageJson.text);
            GUILayout.EndScrollView();

            GUILayout.Space(10);

            GUILayout.Label("Package Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Name", package.name);
            EditorGUILayout.LabelField("Version", package.version);

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Increment Version");

                if (GUILayout.Button("Major 1.0.0", EditorStyles.miniButtonLeft))
                    NpmUtils.ExecuteNpmCommand($"version major", (code, result) => Refresh());

                if (GUILayout.Button("Minor 0.1.0", EditorStyles.miniButtonMid))
                    NpmUtils.ExecuteNpmCommand($"version minor", (code, result) => Refresh());

                if (GUILayout.Button("Patch 0.0.1", EditorStyles.miniButtonRight))
                    NpmUtils.ExecuteNpmCommand($"version patch", (code, result) => Refresh());
            }

            EditorGUILayout.LabelField("Directory", directory);

            GUILayout.Space(10);

            if (GUILayout.Button("Publish", GUILayout.Height(24)))
            {
                var msg = $"Are you really want to publish package {package.name}?";
                if (EditorUtility.DisplayDialog("Npm", msg, "Publish", "Cancel"))
                {
                    NpmUtils.ExecuteNpmCommand($"publish --registry {registry}", (code, result) => Refresh());
                }
            }
        }

        private void DrawToolbar()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Registry", EditorStyles.toolbarButton);
                GUILayout.Label(registry, EditorStyles.toolbarButton);
                if (GUILayout.Button("Edit", EditorStyles.toolbarButton))
                {
                    registry = string.Empty;
                    userFetched = false;
                    user = string.Empty;
                    RefreshImmediate();
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
                        registry = registryInput;
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
                    GUILayout.Label("1. Open terminal and run");
                    GUILayout.TextField($"npm adduser --registry {registry}");
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
    }
}