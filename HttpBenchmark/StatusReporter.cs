using Akka.Actor;
using System;

namespace HttpBenchmark
{
    /// <summary>
    /// Responsible for printing reports based on various stages or elapsed time
    /// </summary>
    public class StatusReporter : ReceiveActor
    {
        private class Tick { }

        private Summary lastKnownSummary;
        private bool reportedFinish;

        public StatusReporter()
        {
            reportedFinish = false;
            var tick = new Tick();

            Context.System.Scheduler.ScheduleTellRepeatedly(
                initialDelay: TimeSpan.FromSeconds(2),
                interval: TimeSpan.FromSeconds(2),
                receiver: Self,
                message: tick,
                sender: Self);

            Receive<Summary>(s => ReportSummary(s));
            Receive<Tick>(_ => ReportPeriodicalStatus());
            Receive<string>(s => Console.WriteLine(s));
        }

        private void ReportSummary(Summary summary)
        {
            lastKnownSummary = summary;
            if (summary.NothingRequestedYet())
                Console.WriteLine("Starting to download {0} requests...", summary.NrRequestsToDo);
            else if (summary.AllResponsesReceived())
            {
                if (!reportedFinish)
                    Console.WriteLine("Finished downloading.");
                reportedFinish = true;
            }
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
