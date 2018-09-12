using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Dangl.EvaluationPackageGenerator
{
    class Program
    {
        public const string MYGET_USERNAME = "georgdangl";

        static void Main(string[] args)
        {
            Console.WriteLine("Please provide the MyGet ApiKey as first parameter and the path to the readme as second parameter.");
            Console.WriteLine("Generating packages...");
            var apiKey = args[0];
            var readmePath = args[1];

            CreateEvaluationPackage(apiKey, readmePath)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            Console.WriteLine("Package generated");
        }

        static async Task CreateEvaluationPackage(string apiKey, string readmePath)
        {
            var packages = new[]
            {
                "Dangl.AVA",
                "Dangl.AVA.Converter",
                "Dangl.AVA.Converter.Excel",
                "Dangl.AVA.IO",
                "Dangl.AVACloud.Client",
                "Dangl.AVACloud.Client.Shared",
                "Dangl.GAEB",
                "Dangl.Oenorm"
            };
            var packageVersions = new Dictionary<string, string>();

            var packageFolder = Path.Combine(Path.GetDirectoryName(readmePath), $"{DateTime.Now:yyyyMMdd}_Dangl.AVA");
            if (!Directory.Exists(packageFolder))
            {
                Directory.CreateDirectory(packageFolder);
            }
            foreach (var package in packages)
            {
                var version = await DownloadPackage(package, apiKey, packageFolder);
                packageVersions.Add(package, version);
            }

            var destReadmePath = Path.Combine(packageFolder, "README.txt");

            File.Copy(readmePath, destReadmePath);

            var packageInfos = $"This package was created at {DateTime.Now:dd.MM.yyyy HH:mm}";
            packageInfos += Environment.NewLine + "The following versions were used:" + Environment.NewLine;

            foreach (var package in packages)
            {
                packageInfos += package + ": " + packageVersions[package] + Environment.NewLine;
            }

            var packageInfosPath = Path.Combine(packageFolder, "_packinfo.txt");
            using (var fs = File.CreateText(packageInfosPath))
            {
                await fs.WriteAsync(packageInfos);
            }
        }

        static async Task<string> DownloadPackage(string packageName, string apiKey, string packageFolder)
        {
            Console.WriteLine("Generating " + packageName + "...");

            var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers
                .AuthenticationHeaderValue("Basic", GetBasicAuthValue(apiKey));

            var url = "https://www.myget.org/F/dangl-ava/api/v3/query";

            var packagesJson = await client.GetStringAsync(url);
            var packages = JObject.Parse(packagesJson);

            var packageVersion = (string)((packages["data"] as JArray)
                .First(t => (string)t["id"] == packageName)
                ["version"]);

            var packageDownloadLink = $"https://www.myget.org/F/dangl-ava/api/v2/package/{packageName}/{packageVersion}";

            var packagePath = Path.Combine(packageFolder, packageName + "." + packageVersion + ".nupkg");
            using (var packageStream = await client.GetStreamAsync(packageDownloadLink))
            {
                using (var fs = File.Create(packagePath))
                {
                    await packageStream.CopyToAsync(fs);
                }
            }

            using (var packageStream = File.Open(packagePath, FileMode.Open))
            {
                using (var zipArchive = new ZipArchive(packageStream))
                {
                    var net45Entries = zipArchive.Entries
                        .Where(e => e.Length > 0)
                        .Where(e => e.FullName.StartsWith("lib/net45"));
                    foreach(var net45Entry in net45Entries)
                    {
                        var entryRelative = net45Entry.FullName
                            .Replace("lib/net452/", string.Empty)
                            .Replace("lib/net45/", string.Empty);

                        var entryPath = Path.Combine(packageFolder, "net45dlls", packageName, entryRelative);

                        if (!Directory.Exists(Path.GetDirectoryName(entryPath)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(entryPath));
                        }

                        using (var fs = File.Create(entryPath))
                        {
                            using (var entryStream = net45Entry.Open())
                            {
                                await entryStream.CopyToAsync(fs);
                            }
                        }
                    }
                }
            }

            return packageVersion;
        }

        private static string GetBasicAuthValue(string apiKey)
        {
            return Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                .GetBytes(MYGET_USERNAME + ":" + apiKey));
        }
    }
}
