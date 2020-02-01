using UnityEditor;
using UnityEngine;

namespace NpmPackageLoader.Sources.Editor
{
    internal static class Styles
    {
        public static readonly GUIStyle HeaderDisplayNameLabel;
        public static readonly GUIStyle HeaderNameLabel;
        public static readonly GUIStyle HeaderVersionLabel;
        public static readonly GUIStyle BigTitle;

        static Styles()
        {
            BigTitle = new GUIStyle("IN BigTitle");
            var border = BigTitle.margin;
            border.top = 0;
            BigTitle.margin = border;

            HeaderDisplayNameLabel = new GUIStyle(EditorStyles.largeLabel)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 17,
                margin = new RectOffset(5, 5, 5, 0),
            };
            HeaderVersionLabel = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 16,
                margin = new RectOffset(0, 0, 5, 5),
            };
            HeaderNameLabel = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 13,
                margin = new RectOffset(5, 5, 0, 5),
            };
        }
    }
}