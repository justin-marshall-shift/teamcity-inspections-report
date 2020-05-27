using Newtonsoft.Json;

namespace ToolKit.Common.Jira
{
    public class JiraIssueResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("self")]
        public string Self { get; set; }
    }

    public class JiraIssueRequest
    {
        [JsonProperty("update")]
        public JiraUpdate Update { get; set; }
        [JsonProperty("fields")]
        public JiraFields Fields { get; set; }
    }

    public class JiraFields
    {
        [JsonProperty("summary")]
        public string Summary { get; set; }
        [JsonProperty("parent")]
        public string Parent { get; set; }
        [JsonProperty("issueType")]
        public JiraIdentifier IssueType { get; set; }
        [JsonProperty("components")]
        public JiraIdentifier[] Components { get; set; }
        [JsonProperty("project")]
        public JiraIdentifier Project { get; set; }
        [JsonProperty("reporter")]
        public JiraIdentifier Reporter { get; set; }
        [JsonProperty("priority")]
        public JiraIdentifier Priority { get; set; }
        [JsonProperty("labels")]
        public string[] Labels { get; set; }
        [JsonProperty("assignee")]
        public JiraIdentifier Assignee { get; set; }
        [JsonProperty("description")]
        public JiraIssueDescription Description { get; set; }

    }

    public class JiraUpdate
    {
    }

    public class JiraIssueDescription : JiraIssueDescriptionContent
    {
        [JsonProperty("version")]
        public int Version { get; set; }
    }

    public class JiraIssueDescriptionContent
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("content")]
        public JiraIssueDescriptionContent[] Content { get; set; }
    }

    public class JiraIssueParent
    {
        [JsonProperty("key")]
        public string Key { get; set; }
    }

    public class JiraIdentifier
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
