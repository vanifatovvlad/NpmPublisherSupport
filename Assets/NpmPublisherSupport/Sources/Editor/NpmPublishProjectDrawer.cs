using System.Collections.Generic;

namespace NpmPublisherSupport
{
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public static class NpmPublishProjectDrawer
    {
        private static List<string> _paths = new List<string>();

        static NpmPublishProjectDrawer()
        {
            EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGUI;
        }

        private static void ProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            if (selectionRect.height > 30)
                return;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var packageJson = NpmPublishMenu.GetPackageJson(path);
            if (packageJson == null)
            {
                _paths.Remove(path);
                return;
            }

            foreach (var existsPath in _paths)
            {
                if (path.Length != existsPath.Length && path.StartsWith(existsPath))
                    return;
            }

            if (!_paths.Contains(path))
            {
                _paths.Add(path);
            }

            var rect = new Rect(selectionRect)
            {
                xMin = selectionRect.xMax - 30,
                xMax = selectionRect.xMax - 4,
            };

            GUI.Label(rect, "npm", Styles.RightGrayLabel);
        }
    }
}