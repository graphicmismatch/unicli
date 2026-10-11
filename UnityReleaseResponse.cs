namespace unicli;

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public struct Download
    {
        public string url { get; set; }
        public string type { get; set; }
        public string platform { get; set; }
        public string architecture { get; set; }
        public DownloadSize downloadSize { get; set; }
        public InstalledSize installedSize { get; set; }
        public List<Module> modules { get; set; }
        public string integrity { get; set; }
    }

    public struct DownloadSize
    {
        public object value { get; set; }
        public string unit { get; set; }
    }

    public struct Eula
    {
        public string url { get; set; }
        public string type { get; set; }
        public string label { get; set; }
        public string message { get; set; }
    }

    public struct ExtractedPathRename
    {
        public string from { get; set; }
        public string to { get; set; }
    }

    public struct InstalledSize
    {
        public object value { get; set; }
        public string unit { get; set; }
    }

    public struct Module
    {
        public string id { get; set; }
        public string slug { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string category { get; set; }
        public string url { get; set; }
        public string type { get; set; }
        public DownloadSize downloadSize { get; set; }
        public InstalledSize installedSize { get; set; }
        public bool required { get; set; }
        public bool hidden { get; set; }
        public bool preSelected { get; set; }
        public string integrity { get; set; }
        public List<Module> subModules { get; set; }
        public string destination { get; set; }
        public List<Eula> eula { get; set; }
        public ExtractedPathRename? extractedPathRename { get; set; }
        public string cmd { get; set; }
    }

    public struct ReleaseNotes
    {
        public string url { get; set; }
        public string type { get; set; }
    }

    public struct Result
    {
        public string version { get; set; }
        public DateTime releaseDate { get; set; }
        public ReleaseNotes releaseNotes { get; set; }
        public List<Download> downloads { get; set; }
        public string skuFamily { get; set; }
        public bool recommended { get; set; }
        public string unityHubDeepLink { get; set; }
        public string shortRevision { get; set; }
        public List<ThirdPartyNotice> thirdPartyNotices { get; set; }
        public string stream { get; set; }

        public override string ToString()
        {
            return $"{version} ({stream})\t{releaseDate.Date.ToString("MM/dd/yyyy")}\t{(recommended ? 'y' : 'n')}";
        }
    }

    public struct UnityReleaseResponse
    {
        public int offset { get; set; }
        public int limit { get; set; }
        public int total { get; set; }
        public List<Result> results { get; set; }
    }

    public struct ThirdPartyNotice
    {
        public string originalFileName { get; set; }
        public string url { get; set; }
        public string type { get; set; }
    }

