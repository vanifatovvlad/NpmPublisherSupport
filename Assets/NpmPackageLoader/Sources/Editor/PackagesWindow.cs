using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NpmPackageLoader.Sources.Editor
{
    public class PackagesWindow : EditorWindow
    {
        private Vector2 _scroll;

        [MenuItem("Window/Npm Package Loader", priority = 500)]
        public static void ShowPackagesWindow()
        {
            PackageDatabase.RefreshPackagesToUpdate();
            
            var window = Resources.FindObjectsOfTypeAll<PackagesWindow>().FirstOrDefault()
                         ?? CreateInstance<PackagesWindow>();

            window.titleContent = new GUIContent("Npm Package Loader");
            window.ShowUtility();
            window.Repaint();
        }

        private void OnGUI()
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
            {
                DrawToolbar();
                DrawContent();
            }
        }

        private void DrawToolbar()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                {
                    PackageDatabase.RefreshPackagesToUpdate();
                    Repaint();
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawContent()
        {
            if (PackageDatabase.Packages.Count == 0)
            {
                EditorGUILayout.HelpBox("No additional downloads required", MessageType.Info);
                return;
            }
            
            var color = GUI.color;
            GUI.color *= new Color(1f, 0.80f, 0f);
            if (PackageDatabase.Packages.Any(p => p.IsNotInstalled || p.IsReadyForUpdate))
            {
                EditorGUILayout.HelpBox("Some packages requires download additional unityPackage", MessageType.Warning);
            }

            GUI.color = color;

            _scroll = GUILayout.BeginScrollView(_scroll);
            
            var drawHeader = true;
            foreach (var info in PackageDatabase.NotInstalledPackages)
            {
                DrawHeaderIfNeed(ref drawHeader, "Not installed");
                DrawPackage(info);
            }

            drawHeader = true;
            foreach (var info in PackageDatabase.UpdateReadyPackages)
            {
                DrawHeaderIfNeed(ref drawHeader, "UUpdate available");
                DrawPackage(info);
            }

            drawHeader = true;
            using (new EditorGUI.DisabledScope(true))
            {
                foreach (var info in PackageDatabase.UpToDatePackages)
                {
                    DrawHeaderIfNeed(ref drawHeader, "Up to date");
                    DrawPackage(info);
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawHeaderIfNeed(ref bool drawHeader, string header)
        {
            if (!drawHeader) return;
            GUILayout.Label(header, EditorStyles.boldLabel);
            drawHeader = false;
        }

        private void DrawPackage(PackageUpdateInfo info)
        {
            using (new GUILayout.HorizontalScope(Styles.BigTitle, GUILayout.Height(60)))
            {
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(info.Package.displayName, Styles.HeaderDisplayNameLabel,
                        GUILayout.ExpandWidth(false));
                    GUILayout.Label(info.LoaderName, Styles.HeaderVersionLabel);
                    GUILayout.EndHorizontal();
                    GUILayout.Label(info.Package.name, Styles.HeaderNameLabel);
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(info.IsUpToDate))
                {
                    GUILayout.BeginVertical();
                    var action = info.IsNotInstalled ? $"Install {info.AvailableVersion}"
                        : info.IsReadyForUpdate ? $"Update to {info.AvailableVersion}"
                        : "Up to date";
                    if (GUILayout.Button(action, GUILayout.Width(120), GUILayout.Height(45)))
                    {
                        DoInstall(info);
                    }

                    GUILayout.EndVertical();
                }
            }
        }

        private void DoInstall(PackageUpdateInfo info)
        {
            PackageDatabase.InstallOrUpdate(info);
        }
    }
}