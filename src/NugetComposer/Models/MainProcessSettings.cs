namespace NugetComposer.Models
{
    public class MainProcessSettings
    {
        public PackageConfig PackageConfig { get; set; }
        public string TmpExtraction { get; set; }
        public string TmpDownload { get; set; }
        public string Destination { get; set; }
        public bool CleanDownload { get; set; }
        public bool CleanExtraction { get; set; }
        public bool CleanAllBefore { get; set; }
        public bool CleanAllAfter { get; set; }
        public bool CleanDestination { get; set; }
        public string PackageConfigSource { get; set; }
    }
}