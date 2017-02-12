namespace HttpBenchmark
{
    // start manager or downloader
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
