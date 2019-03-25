using UnityEditor;
using UnityEngine;

namespace NpmPublisherSupport
{
    internal static class Styles
    {
        public static readonly GUIStyle CenteredLargeLabel;
        public static readonly GUIStyle BigTitle;
        public static readonly GUIStyle BigTitleWithPadding;

        static Styles()
        {
            BigTitle = new GUIStyle("IN BigTitle");
            var border = BigTitle.margin;
            border.top = 0;
            BigTitle.margin = border;

            BigTitleWithPadding = new GUIStyle(BigTitle)
            {
                padding = new RectOffset(30, 30, 30, 30)
            };

            CenteredLargeLabel = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
        }
    }
}