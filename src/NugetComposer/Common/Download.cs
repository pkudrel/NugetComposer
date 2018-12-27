using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ChromeRuntimeDownloader.Vendors.ShellProgressBar;

namespace NugetComposer.Common
{
    public static class Download
    {
        public static async Task DownloadFileAsync(string fileUrl, string dst)
        {
            var downloadLink = new Uri(fileUrl);
            var file = Path.GetFileName(dst);

            using (var pb = new ProgressBar($"Downloading '{file}' ... "))
            {
                void DownloadProgressChangedEvent(object s, DownloadProgressChangedEventArgs e)
                {
                    pb.Report((double) e.ProgressPercentage / 100);
                }

                using (var webClient = new WebClient())
                {
                    webClient.DownloadProgressChanged += DownloadProgressChangedEvent;
                    await webClient.DownloadFileTaskAsync(downloadLink, dst);
                }


                pb.Finish();
            }

           
        }
    }
}