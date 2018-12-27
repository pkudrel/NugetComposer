namespace NugetComposer.Models
{
    public class NugetProcessResult
    {
        public NugetProcessResult(NugetDefinition nugetDefinition)
        {
            NugetDefinition = nugetDefinition;
        }

        public NugetDefinition NugetDefinition { get; set; }
        public string NugetLocalFile { get; set; }
        public string NugetLocalExtractionDir { get; set; }

        public void SetLocalFile(string dstFile)
        {
            NugetLocalFile = dstFile;
        }

        public void SetExtractionDir(string dstDir)
        {
            NugetLocalExtractionDir = dstDir;
        }
    }
}