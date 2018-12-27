using System;
using System.IO;
using System.IO.Compression;
using ChromeRuntimeDownloader.Vendors.ShellProgressBar;

namespace NugetComposer.Common
{
    public static class Extract
    {
        public static string ExtractZipToDirectory(string zipFile, string dstDirectory)
        {
            return ExtractZipToDirectoryImpl(zipFile, dstDirectory);
        }

        private static string ExtractZipToDirectoryImpl(string zipFile, string dstDirectory)
        {
            if (!File.Exists(zipFile))
            {
                var a = zipFile;
            }

            using (var source = ZipFile.OpenRead(zipFile))
            {
                var file = Path.GetFileName(zipFile);

                var di = Directory.CreateDirectory(dstDirectory);
                var destinationDirectoryFullPath = di.FullName;

                var count = 0;
                var numberEntries = source.Entries.Count;

                using (var pb = new ProgressBar($"Extracting '{file}' ... "))
                {
                    foreach (var entry in source.Entries)
                    {
                        count++;
                        var fileDestinationPath =
                            Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, entry.FullName));

                        if (!fileDestinationPath.StartsWith(destinationDirectoryFullPath,
                            StringComparison.OrdinalIgnoreCase))
                            throw new IOException("File is extracting to outside of the folder specified.");

                        pb.Report(GetNormalizedValue(numberEntries, count));

                        if (Path.GetFileName(fileDestinationPath).Length == 0)
                        {
                            if (entry.Length != 0)
                                throw new IOException("Directory entry with data.");

                            Directory.CreateDirectory(fileDestinationPath);
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath) ??
                                                      throw new InvalidOperationException());
                            entry.ExtractToFile(fileDestinationPath, true);
                        }
                    }

                    pb.Finish();
                }

                return di.FullName;
            }
        }

        private static double GetNormalizedValue(int max, int current)
        {
            return (double) current * 100 / max;
        }
    }
}