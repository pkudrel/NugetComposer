using System.IO;
using Newtonsoft.Json;

namespace AbcVersion
{
    public class AbcLoader
    {
        public static void Read(string root)
        {
            var path = Path.Combine(root, ".abcversion.json");
            var json = File.ReadAllText(path);
            var o = JsonConvert.DeserializeObject<Config>(json);
        }
    }
}