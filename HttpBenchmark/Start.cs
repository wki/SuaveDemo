namespace HttpBenchmark
{
    /// <summary>
    /// Command message instructing Manager or Downloader to start
    /// </summary>
    public class Start
    {
        private static Start instance;
        public static Start Instance
        {
            get
            {
                return instance
                    ?? (instance = new Start());
            }
        }
    }
}
