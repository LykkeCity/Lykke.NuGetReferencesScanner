using System;

namespace Lykke.NuGetReferencesScanner.Domain
{
    public sealed class PackageReference
    {
        public string Name { get; }
        public Version Version { get; }


        private PackageReference(string name, Version version)
        {
            Name = name;
            Version = version;
        }

        public static PackageReference Parse(string name, string version)
        {
            return new PackageReference(name, Version.Parse(version));
        }

        private bool Equals(PackageReference other)
        {
            return string.Equals(Name, other.Name) && Equals(Version, other.Version);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PackageReference)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Version != null ? Version.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return $"{Name} {Version}";
        }
    }
}