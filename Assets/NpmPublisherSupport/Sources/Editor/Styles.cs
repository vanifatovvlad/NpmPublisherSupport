using UnityEditor;
using UnityEngine;

namespace NpmPublisherSupport
{
    internal static class Styles
    {
        public static readonly GUIStyle HeaderDisplayNameLabel;
        public static readonly GUIStyle HeaderNameLabel;
        public static readonly GUIStyle HeaderVersionLabel;
        public static readonly GUIStyle CenteredLargeLabel;
        public static readonly GUIStyle RightGrayLabel;
        public static readonly GUIStyle BigTitle;
        public static readonly GUIStyle BigTitleWithPadding;
        public static readonly GUIStyle RichNoPaddingLabel;

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

            BigTitleWithPadding = new GUIStyle(BigTitle)
            {
                padding = new RectOffset(30, 30, 30, 30)
            };

            CenteredLargeLabel = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            RightGrayLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleRight,
            };

            RichNoPaddingLabel = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                padding = new RectOffset(),
                margin = new RectOffset(2, 2, 2, 1),
            };
        }
    }
}