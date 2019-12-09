using System;

namespace NpmPublisherSupport
{
    [Serializable]
    public class Package
    {
        public string name = "";
        public string version = "";
        public string displayName = "";
        
        public PackagePublishConfig publishConfig = new PackagePublishConfig();
    }

    [Serializable]
    public class PackagePublishConfig
    {
        public string registry;
    }
}