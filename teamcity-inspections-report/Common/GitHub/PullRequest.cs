﻿using System;
using Newtonsoft.Json;

namespace teamcity_inspections_report.Common.GitHub
{
    public class PullRequest
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("number")]
        public int Number { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("created_at")]
        public DateTime Creation { get; set; }
        [JsonProperty("head")]
        public PullRequestHead Head { get; set; }


        internal string LastDevelopCommit { get; set; }
        internal DateTime? LastDevelopMerge { get; set; }
    }

    public class PullRequestHead
    {
        [JsonProperty("ref")]
        public string Reference { get; set; }
        [JsonProperty("sha")]
        public string Commit { get; set; }
    }
}
