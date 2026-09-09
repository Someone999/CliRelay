using System.Text.Json.Serialization;

namespace CliRelay.Processes;


public class RedirectionInfo
{
    [JsonPropertyName("stderr")]
    public bool RedirectStandardError { get; init; }
    
    [JsonPropertyName("stdin")]
    public bool RedirectStandardInput { get; init; }
    
    [JsonPropertyName("stdout")]
    public bool RedirectStandardOutput { get; init; }
}