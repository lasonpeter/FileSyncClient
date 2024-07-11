using Newtonsoft.Json;

namespace FileSyncClient.Config;

public class Settings
{
    [JsonProperty("port")] public int Port { get; set; } = 11000;

    [JsonProperty("host_name")] public string HostName { get; set; } = "localhost";

    [JsonProperty("synchronization_paths")] public List<SynchronizedObject> SynchronizedObjects { get; set; }

    [JsonProperty("working_directory")] public string WorkingDirectory;
} 