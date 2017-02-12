namespace HttpBenchmark
{
    // total summary
    public class Summary
    {
        public int NrRequestsToDo { get; set; }
        public int NrRequestsSent { get; set; }
        public int NrResponsesReceived { get; set; }

        public double TotalMilliseconds { get; set; }

        public Summary(int nrRequestsToDo)
        {
            NrRequestsToDo = 0;
            NrRequestsSent = 0;
            NrResponsesReceived = 0;
            TotalMilliseconds = 0;
        }

        public bool NothingRequestedYet() =>
            NrRequestsSent == 0;

        public bool NeedMoreRequests() =>
            NrRequestsSent < NrRequestsToDo;

        public bool AllResponsesReceived() =>
            NrResponsesReceived >= NrRequestsToDo;

        public void AddResult(Result result)
        {
            NrRequestsSent++;
            TotalMilliseconds += result.TotalMilliseconds;
            // content length?
        }
    }
}
