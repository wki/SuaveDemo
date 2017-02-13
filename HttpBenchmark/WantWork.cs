namespace HttpBenchmark
{
    /// <summary>
    /// Query message from downloader to manager to request a unit of work
    /// </summary>
    public class WantWork
    {
        private static WantWork instance;
        public static WantWork Instance
        {
            get
            {
                return instance 
                    ?? (instance = new WantWork());
            }
        }
    }
}
