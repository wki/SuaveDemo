namespace HttpBenchmark
{
    // request an Url to download
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
