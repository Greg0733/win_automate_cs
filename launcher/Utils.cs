using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using launcher.ComponentsManagers;

namespace launcher
{
    internal readonly struct ProgressData(double? progress = null, string? message = null, bool exception = false, bool finished = false)
	{
        public readonly double? progress = progress;
        public readonly string? message = message;
        public readonly bool exception = exception;
        public readonly bool finished = finished;
	}
	internal class Utils
    {
        internal static void DictOfSetsAdd<TKey, TVal, TSet>(IDictionary<TKey, TSet> dict, TKey key, TVal val)
            where TSet : ISet<TVal>, new()
        {
            if (!dict.ContainsKey(key))
            {
                dict[key] = new TSet();
            }
            dict[key].Add(val);
        }

        internal static async Task DownloadAndExtractAsync(string url, string extractDirPath, IProgress<string>? progress = null)
        {
            progress?.Report("Downloading");
            using Stream filesStream = await ComponentManager.httpClientInstance.GetStreamAsync(new Uri(url));
            using ZipArchive zipArchive = new(filesStream);

            progress?.Report("Extracting");
            // CreateDirectory does nothing if the directory already exists
            Directory.CreateDirectory(extractDirPath);
            zipArchive.ExtractToDirectory(extractDirPath, true);
        }
    }
}
