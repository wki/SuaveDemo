using System;
using Akka.Actor;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace HttpBenchmark
{
    // via Broadcast informed about start/stop
    public class Downloader : ReceiveActor
    {
        private Stopwatch stopwatch;
        private IActorRef manager;

        public Downloader(IActorRef manager)
        {
            this.manager = manager;

            Receive<Start>(_ => Sender.Tell(WantWork.Instance));
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

            manager.Tell(Result.From(s, stopwatch));
            manager.Tell(WantWork.Instance);
        }
    }
}
