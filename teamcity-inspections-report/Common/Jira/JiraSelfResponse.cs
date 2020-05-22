using Newtonsoft.Json;

namespace teamcity_inspections_report.Common.Jira
{
    public class JiraSelfResponse
    {
        [JsonProperty("active")]
        public bool IsActive { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
}
