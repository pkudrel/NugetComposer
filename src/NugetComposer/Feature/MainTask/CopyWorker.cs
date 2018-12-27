using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NugetComposer.Common;
using NugetComposer.Models;

namespace NugetComposer.Feature.MainTask
{
    public class CopyWorker
    {
        private readonly string _dst;

        public CopyWorker(string dst)
        {
            _dst = dst;
        }

        public async Task CopyOne(NugetProcessResult nugetProcessResult)
        {
            var list = GetFilesToCopy(nugetProcessResult);
            await Io.CopyFilesAsync(list, $"Copying '{nugetProcessResult.NugetDefinition.Name}' ... ");
        }

        private List<(string src, string dst)> GetFilesToCopy(NugetProcessResult nugetProcessResult)
        {
            var filesToCopy = new List<(string src, string dst)>();

            foreach (var copyPath in nugetProcessResult.NugetDefinition.CopyPaths)
            {
                var s = copyPath.Src.StartsWith("/") ? copyPath.Src.Substring(1) : copyPath.Src;
                var d = copyPath.Dst.StartsWith("/") ? copyPath.Dst.Substring(1) : copyPath.Dst;
                var src1 = Path.Combine(nugetProcessResult.NugetLocalExtractionDir, s);
                var dst1 = Path.Combine(_dst, d);
                Io.CreateDirIfNotExist(dst1);
                var files = GetFiles(src1);
                var copyList = CreateCopyList(src1, dst1, files);
                filesToCopy.AddRange(copyList);
            }

            return filesToCopy;
        }

        private List<(string src, string dst)> CreateCopyList(string srcDir, string dstDir, List<string> files)
        {
            var list = new List<(string src, string dst)>();

            foreach (var file in files)
            {
                var srcFullPath = Path.Combine(srcDir, file);
                var dst = Path.Combine(dstDir, file);
                list.Add((srcFullPath, dst));
            }


            return list;
        }

        public List<string> GetFiles(string dir)
        {
            var list = new List<string>();
            var di = new DirectoryInfo(dir);
            list.AddRange(di.GetFiles().Select(x => x.Name));
            return list;
        }
    }
}