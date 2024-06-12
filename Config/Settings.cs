using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace FileSyncClient.Config;

public class Settings
{
    [JsonProperty("port")]
    public int Port { get; set; }

    [JsonProperty("host_name")] 
    public string HostName { get; set; } = "localhost";

    [JsonProperty("synchronization_paths")]
    public List<SynchronizedObject> SynchronizedObjects { get; set; }
}