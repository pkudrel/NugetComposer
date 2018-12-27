using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NugetComposer.Common;
using NugetComposer.Common.Bootstrap;
using NugetComposer.Models;

namespace NugetComposer.Feature.MainTask
{
    public class MainProcess
    {
        private readonly AppEnvironment _env;

        public MainProcess(AppEnvironment env)
        {
            _env = env;
        }

        public async Task Do(MainProcessSettings mpc)
        {
            var work = mpc.PackageConfig.Nugets.Select(x => new NugetProcessResult(x)).ToList();
            if (mpc.CleanAllBefore) CleanAll(mpc);
            await StepDownloadNugets(mpc, work);
            StepExtractNugets(mpc, work);
            await StepCopyFiles(mpc, work);
            if (mpc.CleanAllAfter) CleanAll(mpc);
        }


        private void CleanAll(MainProcessSettings mpc)
        {
            if (Directory.Exists(mpc.TmpDownload)) Io.RemoveFolder(mpc.TmpDownload);
            if (Directory.Exists(mpc.TmpExtraction)) Io.RemoveFolder(mpc.TmpExtraction);
        }


        private async Task StepDownloadNugets(
            MainProcessSettings mpc,
            List<NugetProcessResult> items)
        {
            var dst = mpc.TmpDownload;
            if (mpc.CleanDownload) Io.RemoveFolder(dst);
            Io.CreateDirIfNotExist(dst);
            Console.WriteLine($"Download directory: '{dst}'");
            //Console.WriteLine($"Download directory: '{dst}'; Clean: {mpc.CleanDownload}");

            foreach (var item in items)
            {
                var n = item.NugetDefinition;
                var url = $"https://www.nuget.org/api/v2/package/{n.Name}/{n.Version}";
                var fileName = $"{n.Name.ToLower()}.{n.Version.ToLower()}.nupkg";
                var dstFile = Path.Combine(dst, fileName);
                if (File.Exists(dstFile) == false)
                    await Download.DownloadFileAsync(url, dstFile);
                else
                    Console.WriteLine($"Nuget '{fileName}' found on disk. Skip download");
                item.SetLocalFile(dstFile);
            }
        }


        private void StepExtractNugets(
            MainProcessSettings mpc,
            List<NugetProcessResult> items)
        {
            var dst = mpc.TmpExtraction;
            if (mpc.CleanExtraction) Io.RemoveFolder(dst);
            Io.CreateDirIfNotExist(dst);
            Console.WriteLine($"Extract directory: '{dst}'");
            //Console.WriteLine($"Extract directory: '{dst}'; Clean: {mpc.CleanExtraction}");

            foreach (var packagesInfo in items)
            {
                var dstDir = Path.Combine(mpc.TmpExtraction,
                    Path.GetFileNameWithoutExtension(packagesInfo.NugetLocalFile) ??
                    throw new InvalidOperationException());
                Io.CreateDirIfNotExist(dstDir);
                var filePath = Extract.ExtractZipToDirectory(packagesInfo.NugetLocalFile, dstDir);
                packagesInfo.SetExtractionDir(dstDir);
            }
        }

        private async Task StepCopyFiles(MainProcessSettings mpc, List<NugetProcessResult> items)
        {
            var dst = mpc.Destination;
            if (mpc.CleanExtraction) Io.RemoveFolder(dst);
            Io.CreateDirIfNotExist(dst);
            Console.WriteLine($"Extract directory: '{dst}'");
            //Console.WriteLine($"Extract directory: '{dst}'; Clean: {mpc.CleanExtraction}");

            var copyWorker = new CopyWorker(dst);
            foreach (var nugetProcessResult in items) await copyWorker.CopyOne(nugetProcessResult);
        }
    }
}