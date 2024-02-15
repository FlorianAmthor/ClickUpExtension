using System.Linq;
using Newtonsoft.Json;

namespace Codice.Client.IssueTracker.ClickUpExtension.Model;

internal class ClickUpTask
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("custom_id")] public string CustomId { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("text_content")] public string TextContent { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("status")] public ClickUpTaskStatus Status { get; set; }
    [JsonProperty("orderindex")] public string OrderIndex { get; set; }
    [JsonProperty("date_created")] public string DateCreated { get; set; }
    [JsonProperty("date_updated")] public string DateUpdated { get; set; }
    [JsonProperty("date_closed")] public string DateClosed { get; set; }
    [JsonProperty("creator")] public ClickUpUserInfo Creator { get; set; }
    [JsonProperty("assignees")] public ClickUpUserInfo[] Assignees { get; set; }
    
    internal PlasticTask ConvertToPlasticTask()
    {
        return new PlasticTask
        {
            CanBeLinked = true,
            Description = Description,
            Id = Id,
            Owner = Assignees.Length > 0 ? string.Join(", ", Assignees.Select(a => a.UserName)) : "", //Creator.Username,
            RepName = "",
            Status = Status.Status,
            Title = Name
        };
    }

}