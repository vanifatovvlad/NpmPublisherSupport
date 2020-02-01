namespace NpmPackageLoader.Sources.Editor
{
    public class PackageUpdateInfo
    {
        public PackageJson Package;
        public string InstalledVersion;
        public string AvailableVersion;

        public string PackageJsonPath;
        public string LoaderPath;
        public string LoaderName;

        public bool IsNotInstalled => InstalledVersion == null;
        public bool IsInstalled => InstalledVersion != null;
        public bool IsReadyForUpdate => IsInstalled && InstalledVersion != AvailableVersion;
        public bool IsUpToDate => IsInstalled && InstalledVersion == AvailableVersion;
    }
}