using System.Diagnostics;

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

        public static Result From(string s, Stopwatch stopwatch) =>
            new Result(s.Length, stopwatch.Elapsed.TotalMilliseconds);

        public static Result From(int contentLenth, double totalMilliseconds) =>
            new Result(contentLenth, totalMilliseconds);
    }
}
