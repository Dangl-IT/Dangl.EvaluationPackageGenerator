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
    public class EvaluationPackageGenerator
    {
        public const string MYGET_QUERY_URL = "https://www.myget.org/F/dangl-ava/api/v3/query";
        public const string MYGET_FEED_URL = "https://www.myget.org/F/dangl-ava/api/v2/package/";

        private readonly CommandLineOptions _commandLineOptions;

        private HttpClient _httpClient;

        public EvaluationPackageGenerator(CommandLineOptions commandLineOptions)
        {
            _commandLineOptions = commandLineOptions;
        }

        public async Task GeneratePackage()
        {
            var packageVersions = new Dictionary<string, string>();

            var packageFolder = Path.Combine(_commandLineOptions.OutputPath, $"{DateTime.Now:yyyyMMdd}_Dangl.AVA");
            if (!Directory.Exists(packageFolder))
            {
                Directory.CreateDirectory(packageFolder);
            }
            if (File.Exists(_commandLineOptions.ReadmePath))
            {
                var destReadmePath = Path.Combine(packageFolder, "README.txt");
                File.Copy(_commandLineOptions.ReadmePath, destReadmePath);
            }

            SetupHttpClient();
            foreach (var package in PackageNameProvider.PackageNames)
            {
                var version = await DownloadSinglePackage(package, packageFolder, _commandLineOptions.IncludePrerelease);
                packageVersions.Add(package, version);
            }

            var packageInfos = $"This package was created at {DateTime.Now:dd.MM.yyyy HH:mm}";
            packageInfos += Environment.NewLine + "The following versions were used:" + Environment.NewLine;
            packageInfos += "Please contact Dangl IT GmbH at info@dangl-it.com for support" + Environment.NewLine;

            foreach (var package in PackageNameProvider.PackageNames)
            {
                packageInfos += package + ": " + packageVersions[package] + Environment.NewLine;
            }

            var packageInfosPath = Path.Combine(packageFolder, "_packinfo.txt");
            using (var fs = File.CreateText(packageInfosPath))
            {
                await fs.WriteAsync(packageInfos);
            }
        }

        private void SetupHttpClient()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers
                .AuthenticationHeaderValue("Basic", GetBasicAuthValue());
        }

        private async Task<string> DownloadSinglePackage(string packageName, string packageFolder, bool includePrerelease)
        {
            Console.WriteLine("Generating " + packageName + "...");

            var queryUrl = includePrerelease ? $"{MYGET_QUERY_URL}?prerelease=true" : MYGET_QUERY_URL;
            var packagesJson = await _httpClient.GetStringAsync(queryUrl);
            var packages = JObject.Parse(packagesJson);

            var packageVersion = (string)((packages["data"] as JArray)
                .First(t => (string)t["id"] == packageName)
                ["version"]);

            var packageDownloadLink = $"{MYGET_FEED_URL}{packageName}/{packageVersion}";

            var packagePath = Path.Combine(packageFolder, packageName + "." + packageVersion + ".nupkg");
            using (var packageStream = await _httpClient.GetStreamAsync(packageDownloadLink))
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
                    foreach (var net45Entry in net45Entries)
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

        private string GetBasicAuthValue()
        {
            return Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                .GetBytes(_commandLineOptions.MyGetUsername + ":" + _commandLineOptions.MyGetApiKey));
        }
    }
}
