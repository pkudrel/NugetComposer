﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ChromeRuntimeDownloader.Vendors.ShellProgressBar;

namespace NugetComposer.Common
{
    public static class Io
    {
        public static async Task CopyFilesAsync(List<(string src, string dst)> files, string message)
        {
            var count = 0;
            var numberEntries = files.Count;
            using (var pb = new ProgressBar(message))
            {
                foreach (var file in files)
                {
                    count++;
                    await CopyFileAsync(file.src, file.dst);
                    pb.Report(GetNormalizedValue(numberEntries, count));
                }
                pb.Finish();
            }
        }

        public static async Task CopyFileAsync(string sourcePath, string destinationPath)
        {
            using (Stream source = File.Open(sourcePath, FileMode.Open))
            {

                var d = Path.GetDirectoryName(destinationPath);
                Io.CreateDirIfNotExist(d);

                using (Stream destination = File.Create(destinationPath))
                {
                    await source.CopyToAsync(destination);
                }
            }
        }

        private static double GetNormalizedValue(int max, int current)
        {
            return (double) current * 100 / max;
        }


        public static void CreateDirIfNotExist(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static void ClearFolder(string path)
        {
            var dir = new DirectoryInfo(path);

            foreach (var fi in dir.GetFiles())
            {
                fi.IsReadOnly = false;
                fi.Delete();
            }

            foreach (var di in dir.GetDirectories())
            {
                ClearFolder(di.FullName);
                di.Delete();
            }
        }

        public static void RemoveFolder(string path)
        {
            if (Directory.Exists(path))
            {
                var dir = new DirectoryInfo(path);

                foreach (var fi in dir.GetFiles())
                {
                    fi.IsReadOnly = false;
                    fi.Delete();
                }

                foreach (var di in dir.GetDirectories())
                {
                    ClearFolder(di.FullName);
                    di.Delete();
                }
                dir.Delete();
            }
        }

        public static void RemoveFile(string path)
        {
            var fileInfo = new FileInfo(path);


            fileInfo.Delete();
        }
    }
}