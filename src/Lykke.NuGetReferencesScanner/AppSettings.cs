namespace Lykke.NuGetReferencesScanner
{
    public class AppSettings
    {
        public NuGetScannerSettings NuGetScannerSettings { get; set; }
    }

    public class NuGetScannerSettings
    {
        public string ApiKey { get; set; }
    }
}
