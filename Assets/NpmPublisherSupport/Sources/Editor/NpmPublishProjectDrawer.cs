namespace NpmPublisherSupport
{
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public static class NpmPublishProjectDrawer
    {
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
                return;
            }

            var rootFolderPath = NpmPublishMenu.GetPackageRootFolder(packageJson);
            if (rootFolderPath != path)
            {
                return;
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