using Akka.Actor;
using System.Collections.Generic;

namespace HttpBenchmark
{
    // handle all things
    public class Manager : ReceiveActor
    {
        private const string DownloadAgentPrefix = "download-";

        private IActorRef requestor;
        private List<IActorRef> downloaders;

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
            Receive<Start>(s => Start(s));

            downloaders = new List<IActorRef>();
            for (var i = 1; i < options.Concurrency; i++)
            {
                downloaders.Add(
                    Context.ActorOf(Props.Create<Downloader>(Self), $"{DownloadAgentPrefix}{i}")
                );
            }
        }

        private void Start(Start start)
        {
            requestor = Sender;
            downloaders.ForEach(d => d.Tell(start));
        }

        private void DispatchWork()
        {
            if (summary.NeedMoreRequests())
            {
                summary.NrRequestsSent++;
                Sender.Tell(options.Url);
            }
        }

        private void SaveResult(Result result)
        {
            summary.NrResponsesReceived++;
            summary.AddResult(result);

            if (summary.AllResponsesReceived())
            {
                // TODO: print output
                requestor.Tell(summary);
            }
        }
    }
}
