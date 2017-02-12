using Akka.Actor;
using System;

namespace HttpBenchmark
{
    public class StatusReporter : ReceiveActor
    {
        private class Tick { }

        private Summary lastKnownSummary;

        public StatusReporter()
        {
            var tick = new Tick();

            Context.System.Scheduler.ScheduleTellRepeatedly(
                initialDelay: TimeSpan.FromSeconds(2),
                interval: TimeSpan.FromSeconds(2),
                receiver: Self,
                message: tick,
                sender: Self);

            Receive<Summary>(s => ReportSummary(s));
            Receive<Tick>(_ => ReportPeriodicalStatus());
        }

        private void ReportSummary(Summary summary)
        {
            lastKnownSummary = summary;
            if (summary.NothingRequestedYet())
                Console.WriteLine("Starting to download {0} requests...", summary.NrRequestsToDo);
            else if (summary.AllResponsesReceived())
                Console.WriteLine("Finished downloading.");
        }

        private void ReportPeriodicalStatus()
        {
            Console.WriteLine("Sent {0}, received {1}/{2} ({3:N1}%) requests",
                lastKnownSummary.NrRequestsSent,
                lastKnownSummary.NrResponsesReceived,
                lastKnownSummary.NrRequestsToDo,
                100.0 * lastKnownSummary.NrResponsesReceived / lastKnownSummary.NrRequestsToDo
            );
        }
    }
}
