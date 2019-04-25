namespace NpmPublisherSupport
{
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public static class NpmPublishProjectDrawer
    {
        static NpmPublishProjectDrawer()
        {
            //EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGUI;
        }

        private static void ProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            if (selectionRect.height > 30)
                return;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var packageJson = NpmPublishMenu.GetPackageJson(path);
            if (packageJson == null)
                return;

            var rect = new Rect(selectionRect)
            {
                xMin = selectionRect.xMax - 30,
                xMax = selectionRect.xMax - 4,
            };

            var package = JsonUtility.FromJson<Package>(packageJson.text);
            var content = new GUIContent("npm", package.name);

            if (GUI.Button(rect, content, Styles.RightGrayLabel))
            {
                NpmPublishWindow.OpenPublish(packageJson);
            }
        }
    }
}