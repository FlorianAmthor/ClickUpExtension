using Newtonsoft.Json;

namespace Codice.Client.IssueTracker.ClickUpExtension.Model;

internal class ClickUpTaskStatus
{
    [JsonProperty("status")] public string Status { get; set; }
    [JsonProperty("color")] public string Color { get; set; }
    [JsonProperty("orderindex")] public string OrderIndex { get; set; }
    [JsonProperty("type")] public string Type { get; set; }
}