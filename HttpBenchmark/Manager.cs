using Akka.Actor;
using System.Linq;

namespace HttpBenchmark
{
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
}
