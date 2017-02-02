using System;
using Akka.Actor;
using CommandLine;
using Akka.Routing;
using System.Net;
using System.IO;

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
    }

    public class Start { }
    public class WantWork { }
    public class Result { }

    // handle all things
    public class Manager : ReceiveActor
    {
        private IActorRef downloader;

        public Manager(IActorRef downloader, string url)
        {
            this.downloader = downloader;

            Receive<WantWork>(_ => Sender.Tell(url));
            Receive<Result>(r => SaveResult(r));

            downloader.Tell(new Start());
        }

        private void SaveResult(Result result)
        {
            // ...
            Context.System.Terminate();
        }
    }

    // via Broadcast informed about start/stop
    public class Downloader : ReceiveActor
    {
        public Downloader()
        {
            Receive<Start>(_ => Sender.Tell(new WantWork()));
            Receive<Uri>(url => StartDownload(url));
            Receive<WebResponse>(r => HandleWebResponse(r));
            Receive<string>(s => { }); // do nothing
        }

        private void StartDownload(Uri url)
        {
            // start download, wait, pipe result into self
            // var webClient = new System.Net.WebClient();
            var webRequest = WebRequest.Create(url);
            webRequest.GetResponseAsync().PipeTo(Self);
        }

        private void HandleWebResponse(WebResponse response)
        {
            using (var s = response.GetResponseStream())
            using (var reader = new StreamReader(s))
            {
                reader.ReadToEndAsync().PipeTo(Self);
            }
        }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            var options = new ProgramOptions();

            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Console.WriteLine("Something went wrong with the options.");
                Environment.Exit(1);
            }

            using (var system = ActorSystem.Create("system"))
            {

                var downloaderProps =
                    Props.Create<Downloader>()
                         .WithRouter(new BroadcastPool(options.Concurrency));
                var downloader = system.ActorOf(downloaderProps, "downloader");

                var manager = system.ActorOf(Props.Create<Manager>(downloader, options.Url, options.NrRequests), "manager");
            }
        }
    }
}
