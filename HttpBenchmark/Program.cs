using System;
using Akka.Actor;

namespace HttpBenchmark
{
    public class Program
    {
        static void Main(string[] args)
        {
            var options = new ProgramOptions();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
                Environment.Exit(1);

            using (var system = ActorSystem.Create("system"))
            {
                var manager = system.ActorOf(Props.Create<Manager>(options), "manager");

                var summary = manager.Ask<Summary>(Start.Instance).Result;
            }
        }
    }
}
