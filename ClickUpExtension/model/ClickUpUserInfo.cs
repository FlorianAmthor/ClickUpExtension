using Newtonsoft.Json;

namespace Codice.Client.IssueTracker.ClickUpExtension.Model;

internal class ClickUpUserInfo
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("username")] public string UserName { get; set; }
    [JsonProperty("email")] public string Email { get; set; }


    public override string ToString()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}