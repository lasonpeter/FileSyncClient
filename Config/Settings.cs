using Newtonsoft.Json;

namespace FileSyncClient.Config;

public class Settings
{
    [JsonProperty("port")] public int Port { get; set; } = 11000;
    [JsonProperty("host_name")] public string HostName { get; set; } = "localhost";
    [JsonProperty("synchronization_paths")] public List<SynchronizedObject> SynchronizedObjects { get; set; }
    [JsonProperty("working_directory")] public string WorkingDirectory;
    private static Settings? _instance;
    public byte[] Fid = [0];
    public readonly ushort Version = 1;


    public static Settings Instance
    {
        get
        {
            if (_instance is null)
            {
                throw new Exception("Settings not loaded");
            }
            return _instance;
        }
    }
    
    
    public Settings(int port, string hostName)
    {
        Port = port;
        HostName = hostName;
        _instance = this;
    }
}