using System;

namespace NpmPackageLoader
{
    [Serializable]
    public class PackageJson
    {
        public string name = string.Empty;
        public string displayName = string.Empty;
        public string version = string.Empty;
    }
}