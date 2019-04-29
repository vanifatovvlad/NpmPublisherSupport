using System;

namespace NpmPublisherSupport
{
    internal class SemVerHelper
    {
        public static string GenerateVersion(string versionString, NpmVersion version)
        {
            var versionParts = versionString.Split('.');
            var major = int.Parse(versionParts[0]);
            var minor = int.Parse(versionParts[1]);
            var patch = int.Parse(versionParts[2]);

            switch (version)
            {
                case NpmVersion.Major:
                    ++major;
                    minor = 0;
                    patch = 0;
                    break;

                case NpmVersion.Minor:
                    ++minor;
                    patch = 0;
                    break;

                case NpmVersion.Patch:
                    ++patch;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(version));
            }

            return $"{major}.{minor}.{patch}";
        }
    }
}