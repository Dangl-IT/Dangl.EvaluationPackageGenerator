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

            var destReadmePath = Path.Combine(packageFolder, "README.txt");
            if (File.Exists(_commandLineOptions.ReadmePath))
            {
                File.Copy(_commandLineOptions.ReadmePath, destReadmePath);
            }
            else
            {
                File.CreateText(destReadmePath).Write(GetDefaultReadmeContent());
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

        private static string GetDefaultReadmeContent()
        {
            return @"This is an evaluation package for the Dangl.AVA & Dangl.GAEB libraries
It is not intended for commercial use and redistribution to third parties is not allowed.
The libraries and all its content must be deleted after the evaluation has been finished.

This package includes also the Dangl.Oenorm package for the Austrian ÖNorm A2063 standards
and the AVACloud client packages, both are also available without additional charge for
customers of the GAEB & AVA .Net Libraries.

Setup instructions:
German -> https://www.dangl-it.de/artikel/einrichten-von-danglava-danglgaeb-in-visual-studio/
English -> https://www.dangl-it.com/articles/setup-danglava-danglgaeb-in-visual-studio/

Documentation for the libraries is available at:
https://docs.dangl-it.com
You need to have an user account. Please contact info@dangl-it.com to be granted access to the projects.


Sample code can be found at: 
https://github.com/Dangl-IT/Dangl.AVA.Examples


To use the libraries, there are two options:

	1. Use the NuGet packages from a local source
	   To configure a local package in a Visual Studio solution, please add a file called NuGet.config
	   with the following content to the solution root:
	   
	   ﻿<?xml version=""1.0"" encoding=""utf-8""?>
         <configuration>
           <packageSources>
             <clear />
             <add key=""local"" value=""./avapackages"" />
             <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" protocolVersion=""3"" />
           </packageSources>
        </configuration>
		
		Then place all the *.nupkg files in a directory called ""avapackages"" in the solution root. You might have
		to restart Visual Studio for it to pick up the changes.
	
	2. Reference the dlls directly
	   All dlls are in the net45Dlls/ folder relative to this Readme

	   
When referencing the dlls, please make sure that the following NuGet dependencies from the public repository are referenced:
	* Dangl.Calculator (https://www.nuget.org/packages/Dangl.Calculator/)
	* Dangl.TextConverter (https://www.nuget.org/packages/Dangl.TextConverter/)
	* Dangl.Common (https://www.nuget.org/packages/Dangl.Common/)
	* HtmlAgilityPack (https://www.nuget.org/packages/HtmlAgilityPack/)
	* Newtonsoft.Json (https://www.nuget.org/packages/Newtonsoft.Json)
	* Antlr4.Runtime (https://www.nuget.org/packages/Antlr4.Runtime)
	-- The following only when using Dangl.AVA.Converter.Excel
	* ClosedXML (https://www.nuget.org/packages/ClosedXML/)

The AVACloud clients are only published with a netstandard2.0 target. Please contact Dangl IT
for infos how to get the clients compatible with .NET 3.5 or later.
Find out more about the AVACloud GAEB SaaS Webservice:
https://www.dangl-it.com/products/avacloud-gaeb-saas/

---
This package was generated with an automatic tool. Find out more at:
https://github.com/Dangl-IT/Dangl.EvaluationPackageGenerator

---
Dangl IT GmbH
+49 (173) 56 45 689
info@dangl-it.com
www.dangl-it.com
";
        }
    }
}
