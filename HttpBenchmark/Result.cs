namespace HttpBenchmark
{
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
}
