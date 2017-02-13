using Akka.Actor;
using System.Collections.Generic;

namespace HttpBenchmark
{
    /// <summary>
    /// Orchestrating actor controlling the whole downloading process
    /// </summary>
    public class Manager : ReceiveActor
    {
        private const string DownloadAgentPrefix = "download-";

        private IActorRef requestor;
        private List<IActorRef> downloaders;
        private IActorRef statusReporter;
        private IActorRef verboseReporter;

        // what we want
        private ProgramOptions options;

        // what we are doing
        private Summary summary;

        public Manager(ProgramOptions options)
        {
            this.options = options;
            summary = new Summary(options.NrRequests);

            Receive<WantWork>(_ => DispatchWork());
            Receive<Result>(r => SaveResult(r));
            Receive<Start>(_ => StartDownloading());

            statusReporter = Context.ActorOf(Props.Create<StatusReporter>(), "status");
            verboseReporter = options.Verbose ? statusReporter : null;

            verboseReporter?.Tell("Starting up...");
            statusReporter.Tell(summary);

            downloaders = new List<IActorRef>();
            for (var i = 1; i <= options.Concurrency; i++)
            {
                verboseReporter?.Tell($"Starting Downloader #{i}...");
                downloaders.Add(
                    Context.ActorOf(
                        Props.Create<Downloader>(Self, verboseReporter), 
                        $"{DownloadAgentPrefix}{i}"
                    )
                );
            }
        }

        private void StartDownloading()
        {
            verboseReporter?.Tell("Telling downloaders to start");
            requestor = Sender;
            downloaders.ForEach(d => d.Tell(Start.Instance));
        }

        private void DispatchWork()
        {
            verboseReporter?.Tell($"Request from {Sender.Path.Name}");

            if (summary.NeedMoreRequests())
            {
                summary.NrRequestsSent++;
                Sender.Tell(options.Url);
            }
        }

        private void SaveResult(Result result)
        {
            verboseReporter?.Tell($"Result from {Sender.Path.Name}: {result.TotalMilliseconds:N1}ms");

            summary.NrResponsesReceived++;
            summary.AddResult(result);

            statusReporter.Tell(summary);

            if (summary.AllResponsesReceived())
            {
                requestor.Tell(summary);
            }
        }
    }
}
