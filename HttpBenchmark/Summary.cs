namespace HttpBenchmark
{
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
}
