namespace NugetComposer.Models
{
    public class Settings
    {
        public string PackageConfigFile { get; set; }
        public bool Clean { get; set; }
        public string Destination { get; set; }


        public bool CleanDownload { get; set; }
        public bool CleanExtraction { get; set; }
        public bool CleanDestination { get; set; }
        public bool CleanAllBefore { get; set; }
        public bool CleanAllAfter { get; set; }

    }
}