using CommandLine;
using System;
using System.Threading.Tasks;

namespace Dangl.EvaluationPackageGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Dangl.EvaluationPackageGenerator");
            Console.WriteLine("Support: info@dangl-it.com");
            Console.WriteLine("Dangl IT GmbH - www.dangl-it.com");
            Console.WriteLine($"Version {VersionInfo.Version}, Built: {VersionInfo.BuildDateUtc.ToLocalTime():dd.MM.yyyy HH:mm}");
            Console.WriteLine("This tool downloads packages from the 'dangl-ava' feed and bundles them in an evaluation package");

            var parsedOptions = Parser.Default.ParseArguments<CommandLineOptions>(args);

            if (parsedOptions.Tag == ParserResultType.Parsed)
            {
                var options = ((Parsed<CommandLineOptions>)parsedOptions).Value;
                var generator = new EvaluationPackageGenerator(options);
                try
                {
                    Console.WriteLine("Starting generation");
                    await generator.GeneratePackage();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                Console.WriteLine("The given command line arguments could not be succesfully parsed.");
            }

            Console.WriteLine("Generation finished");
        }
    }
}
