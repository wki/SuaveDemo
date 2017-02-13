using System;
using Akka.Actor;

namespace HttpBenchmark
{
    /// <summary>
    /// Main Program controlling all actions via commandline arguments
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            var options = new ProgramOptions();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Environment.Exit(1);
            }

            Console.WriteLine($"Options: n={options.NrRequests}, c={options.Concurrency}, url={options.Url}");

            using (var system = ActorSystem.Create("system"))
            {
                var manager = system.ActorOf(Props.Create<Manager>(options), "manager");
                var summary = manager.Ask<Summary>(Start.Instance).Result;

                var avg = summary.TotalMilliseconds / summary.NrResponsesReceived;
                Console.WriteLine($"Summary: {summary.NrResponsesReceived} received, avg: {avg:N1}ms");
            }

            Console.WriteLine("Stop");
        }
    }
}
