using System;
using CommandLine;
using CommandLine.Text;
using System.Linq;

namespace HttpBenchmark
{
    /// <summary>
    /// Commandline options for our commandline program
    /// </summary>
    public class ProgramOptions
    {
        [Option('n', HelpText = "Nr of Requests", Required = true)]
        public int NrRequests { get; set; }

        [Option('c', HelpText = "Nr of concurrent requests", DefaultValue = 1)]
        public int Concurrency { get; set; }

        [Option('u', HelpText = "Url to download", Required = true)]
        public string Uri { get; set; }

        public Uri Url { get { return new System.Uri(Uri); } }

        [Option('v', HelpText = "Print what happens", DefaultValue = false)]
        public bool Verbose { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText();

            if (this.LastParserState?.Errors.Any() == true)
            {
                var errors = help.RenderParsingErrorsText(this, 2); // indent with two spaces

                if (!string.IsNullOrEmpty(errors))
                {
                    help.AddPreOptionsLine(string.Concat(Environment.NewLine, "ERROR(S):"));
                    help.AddPreOptionsLine(errors);
                }
            }

            return help;
        }
    }
}
