using System;
using CommandLine;
using CommandLine.Text;

namespace HttpBenchmark
{
    public class ProgramOptions
    {
        [Option('n', HelpText = "Nr of Requests", Required = true)]
        public int NrRequests { get; set; }

        [Option('c', HelpText = "Nr of concurrent requests", DefaultValue = 1)]
        public int Concurrency { get; set; }

        [Option('u', HelpText = "Url to download", Required = true)]
        public Uri Url { get; set; }

        [HelpOption]
        public string GetUsage() =>
            HelpText.AutoBuild(this,
                helptext => HelpText.DefaultParsingErrorsHandler(this, helptext));
    }
}
