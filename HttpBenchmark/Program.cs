using System;
using Akka.Actor;
using CommandLine;
using Akka.Routing;
using System.Net;
using System.IO;
using System.Linq;
using CommandLine.Text;
using System.Diagnostics;

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

    // start manager or downloader
    public class Start { }

    // request an Url to download
    public class WantWork { }

    // single download result
    public class Result
    {
        public int ContentLength { get; set; }
        public double TotalMilliseconds { get; set; }

        public Result(int contentLength, double totalMilliseconds)
        {
            ContentLength = contentLength;
            TotalMilliseconds = totalMilliseconds;
        }
    }

    // total summary
    public class Summary
    {
        public int NrDownloads { get; set; }
        public double TotalMilliseconds { get; set; }

        public Summary()
        {
            NrDownloads = 0;
            TotalMilliseconds = 0;
        }

        public void AddResult(Result result)
        {
            NrDownloads++;
            TotalMilliseconds += result.TotalMilliseconds;
            // content length?
        }
    }

    // handle all things
    public class Manager : ReceiveActor
    {
        private IActorRef requestor;

        private ProgramOptions options;
        private Summary summary;
        private int NrRequestsSent;
        private int NrResponsesReceived;

        public Manager(ProgramOptions options)
        {
            this.options = options;
            NrRequestsSent = 0;
            NrResponsesReceived = 0;
            summary = new Summary();

            Receive<WantWork>(_ => DispatchWork());
            Receive<Result>(r => SaveResult(r));
            Receive<Start>(s => Start(s));

            for (var i = 1; i < options.Concurrency; i++)
            {
                Context
                    .ActorOf(Props.Create<Downloader>(Self), $"downloader-{i}");
            }
        }

        private void Start(Start start)
        {
            requestor = Sender;

            Context
                .GetChildren().ToList()
                .ForEach(c => c.Tell(start));
        }

        private void DispatchWork()
        {
            if (NrRequestsSent < options.NrRequests)
            {
                NrRequestsSent++;
                Sender.Tell(options.Url);
            }
        }

        private void SaveResult(Result result)
        {
            NrResponsesReceived++;
            summary.AddResult(result);

            if (NrResponsesReceived >= options.NrRequests)
            {
                // TODO: print output
                requestor.Tell(summary);
            }
        }
    }

    // via Broadcast informed about start/stop
    public class Downloader : ReceiveActor
    {
        private Stopwatch stopwatch;
        private IActorRef manager;

        public Downloader(IActorRef manager)
        {
            this.manager = manager;

            Receive<Start>(_ => Sender.Tell(new WantWork()));
            Receive<Uri>(url => StartDownload(url));
            Receive<WebResponse>(r => HandleWebResponse(r));
            Receive<string>(s => ProcessDownload(s)); // do nothing
        }

        // 1st stage of a download - start web request
        private void StartDownload(Uri url)
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();

            var webRequest = WebRequest.Create(url);
            webRequest.GetResponseAsync().PipeTo(Self);
        }

        // 2nd stage of a download - handle response and start downloading content
        private void HandleWebResponse(WebResponse response)
        {
            using (var s = response.GetResponseStream())
            using (var reader = new StreamReader(s))
            {
                reader.ReadToEndAsync().PipeTo(Self);
            }
        }

        // 3rd and final stage of a download - process result
        private void ProcessDownload(string s)
        {
            stopwatch.Stop();

            manager.Tell(new Result(s.Length, stopwatch.Elapsed.TotalMilliseconds));

            manager.Tell(new WantWork());
        }
    }

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

                var result = manager.Ask<Summary>(new Start()).Result;
            }
        }
    }
}
