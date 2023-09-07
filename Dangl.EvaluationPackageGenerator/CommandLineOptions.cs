using CommandLine;

namespace Dangl.EvaluationPackageGenerator
{
    public class CommandLineOptions
    {
        [Option(Required = true)]
        public string ApiKey { get; set; }

        [Option(Default = "./")]
        public string OutputPath { get; set; }

        [Option(HelpText = "If specified, this file will be placed in the generated package")]
        public string ReadmePath { get; set; }

        [Option(Required = false, Default = false, HelpText = "Whether or not to download the latest prerelease versions or stable versions")]
        public bool IncludePrerelease { get; set; }
    }
}
