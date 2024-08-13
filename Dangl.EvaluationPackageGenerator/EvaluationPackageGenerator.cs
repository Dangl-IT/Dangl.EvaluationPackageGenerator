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
        public const string AVA_FEED_QUERY_URL = "https://packages.dangl.dev/dangl-ava/nuget/v3/query";
        public const string XRECHNUNG_FEED_QUERY_URL = "https://packages.dangl.dev/dangl-xrechnung/nuget/v3/query";
        public const string AVA_FEED_PACKAGES_URL = "https://packages.dangl.dev/dangl-ava/nuget/v3/packages/";
        public const string XRECHNUNG_FEED_PACKAGES_URL = "https://packages.dangl.dev/dangl-xrechnung/nuget/v3/packages/";

        private readonly CommandLineOptions _commandLineOptions;

        private HttpClient _httpClient;

        public EvaluationPackageGenerator(CommandLineOptions commandLineOptions)
        {
            _commandLineOptions = commandLineOptions;
        }

        public async Task GeneratePackage()
        {
            var packageVersions = new Dictionary<string, string>();

            var packageFolder = Path.Combine(_commandLineOptions.OutputPath, $"{DateTime.Now:yyyyMMdd}_Dangl.{_commandLineOptions.PackageType}");
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
            foreach (var package in GetPackageNames())
            {
                var version = await DownloadSinglePackage(package, packageFolder, _commandLineOptions.IncludePrerelease);
                packageVersions.Add(package, version);
            }

            var packageInfos = $"This package was created at {DateTime.Now:dd.MM.yyyy HH:mm}";
            packageInfos += Environment.NewLine + "The following versions were used:" + Environment.NewLine;
            packageInfos += "Please contact Dangl IT GmbH at info@dangl-it.com for support" + Environment.NewLine;

            foreach (var package in GetPackageNames())
            {
                packageInfos += package + ": " + packageVersions[package] + Environment.NewLine;
            }

            var packageInfosPath = Path.Combine(packageFolder, "_packinfo.txt");
            using (var fs = File.CreateText(packageInfosPath))
            {
                await fs.WriteAsync(packageInfos);
            }
        }

        private string[] GetPackageNames()
        {
            return _commandLineOptions.PackageType switch
            {
                PackageType.Ava => PackageNameProvider.AvaPackageNames,
                PackageType.XRechnung => PackageNameProvider.XRechnungPackageNames,
                _ => throw new InvalidOperationException("Unknown package type")
            };
        }

        private void SetupHttpClient()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers
                .AuthenticationHeaderValue("Basic", GetBasicAuthValue());
        }

        private async Task<string> DownloadSinglePackage(string packageName, string packageFolder, bool includePrerelease)
        {
            try
            {
                return await DownloadSinglePackageFromFeedAsync(packageName, packageFolder, includePrerelease, AVA_FEED_QUERY_URL, AVA_FEED_PACKAGES_URL);
            }
            catch (InvalidOperationException) // That means we didn't find the package on the AVA feed
            {
                return await DownloadSinglePackageFromFeedAsync(packageName, packageFolder, includePrerelease, XRECHNUNG_FEED_QUERY_URL, XRECHNUNG_FEED_PACKAGES_URL);
            }
        }

        private async Task<string> DownloadSinglePackageFromFeedAsync(string packageName,
            string packageFolder,
            bool includePrerelease,
            string feedQueryUrl,
            string feedPackagesUrl)
        {
            Console.WriteLine("Generating " + packageName + "...");

            var queryUrl = includePrerelease ? $"{feedQueryUrl}?prerelease=true" : feedQueryUrl;
            var packagesJson = await _httpClient.GetStringAsync(queryUrl);
            var packages = JObject.Parse(packagesJson);

            var packageVersion = (string)((packages["data"] as JArray)
                .First(t => (string)t["id"] == packageName)
                ["version"]);

            var packageDownloadLink = $"{feedPackagesUrl}{packageName.ToLowerInvariant()}/{packageVersion.ToLowerInvariant()}/download";

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
                // When using an API key, the username doesn't matter
                .GetBytes("DanglIT:" + _commandLineOptions.ApiKey));
        }
    }
}
