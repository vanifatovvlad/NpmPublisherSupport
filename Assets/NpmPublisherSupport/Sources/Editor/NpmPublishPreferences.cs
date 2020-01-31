using System;
using UnityEditor;

namespace NpmPublisherSupport
{
    public static class NpmPublishPreferences
    {
        private const string RegistryPrefKey = "codewriter.npm-publisher-support.registry";
        private const string AllRegistriesPrefKey = "codewriter.npm-publisher-support.all-registries";
        private const string UpdateVersionRecursivelyPrefKey = "codewriter.npm-publisher-support.update-recursively";

        public static string NpmPackageLoader => "com.codewriter.npm-package-loader";

        public static string Registry
        {
            get => EditorPrefs.GetString(RegistryPrefKey, "");
            set
            {
                EditorPrefs.SetString(RegistryPrefKey, value);

                if (Array.IndexOf(AllRegistries, value) == -1)
                {
                    var registries = AllRegistries;
                    ArrayUtility.Add(ref registries, value);
                    AllRegistries = registries;
                }
            }
        }

        public static string[] AllRegistries
        {
            get => EditorPrefs.GetString(AllRegistriesPrefKey, "").Split('|');
            set => EditorPrefs.SetString(AllRegistriesPrefKey, string.Join("|", value));
        }

        public static string[] EscapedAllRegistries => EditorPrefs.GetString(AllRegistriesPrefKey, "")
            .Replace('/', '\u2215')
            .Split('|');

        internal static bool UpdateVersionRecursively
        {
            get => EditorPrefs.GetInt(UpdateVersionRecursivelyPrefKey, 1) == 1;
            set => EditorPrefs.SetInt(UpdateVersionRecursivelyPrefKey, value ? 1 : 0);
        }
    }
}