namespace NugetComposer.Feature.Arguments
{
    public class Options
    {
        public Options(string configFile, string destination, bool? clean)
        {
            ConfigFile = configFile;
            Destination = destination;
            Clean = clean;
        }

        public string ConfigFile { get; }

        public string PackageConfig { get; set; }
        public string Destination { get; set; }
        public bool? Clean { get; set; }
    }
}