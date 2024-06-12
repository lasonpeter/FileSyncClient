using Newtonsoft.Json;

namespace FileSyncClient.Config;

public class SynchronizedObject
{
    [JsonProperty("path")]
    public string SynchronizedObjectPath { get; set; }
    [JsonProperty("recursive")]
    public bool Recursive { get; set; }
    [JsonProperty("synchronized")] 
    public bool IsSynchronized { get; set; }
}