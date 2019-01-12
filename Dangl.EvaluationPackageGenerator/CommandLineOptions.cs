using CommandLine;

namespace Dangl.EvaluationPackageGenerator
{
    public class CommandLineOptions
    {
        [Option(Default = "georgdangl", HelpText = "The username under which the packages should be downloaded from the MyGet package feed")]
        public string MyGetUsername { get; set; }

        [Option(Required = true)]
        public string MyGetApiKey { get; set; }

        [Option(Default = "./")]
        public string OutputPath { get; set; }

        [Option(HelpText = "If specified, this file will be placed in the generated package")]
        public string ReadmePath { get; set; }
    }
}
