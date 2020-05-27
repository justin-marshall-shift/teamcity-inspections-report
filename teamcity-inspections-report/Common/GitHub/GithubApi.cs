using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace teamcity_inspections_report.Common.GitHub
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum StatusCheckType
    {
        // ReSharper disable InconsistentNaming
        error,
        failure,
        success,
        pending
        // ReSharper restore InconsistentNaming
    }

    public class StatusCheck
    {
        [JsonProperty("state")]
        public StatusCheckType State { get; set; }
        [JsonProperty("target_url")]
        public string TargetUrl { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("context")]
        public string Context { get; set; }
    }

    public class GithubApi
    {
        private const string Repository = "shift-technology/shift";
        private const string UserAgent = "Shift-Teamcity-Reporter";
        private const string UserAgentVersion = "1.0";

        private const string GithubApiUri = "https://api.github.com";

        static GithubApi()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public GithubApi(string accessToken)
        {
            AccessToken = accessToken;
        }

        private string AccessToken { get; }
        private string BaseUri => $"{GithubApiUri}/repos/{Repository}";

        public async Task SetStatusCheckAsync(string commitHash, StatusCheck statusCheck)
        {
            await CallApiAsync(HttpMethod.Post, $"/statuses/{commitHash}", statusCheck);
        }

        public async Task<PullRequest[]> GetOpenPullRequest()
        {
            var list = new List<PullRequest>();

            var page = 1;
            var count = 0;

            do
            {
                var response = await CallApiAsync(HttpMethod.Get, $"/pulls?state=open&sort=created&direction=desc&page={page}");
                var prs = JsonConvert.DeserializeObject<PullRequest[]>(response);
                list.AddRange(prs);
                count = prs.Length;
                page++;

            } while (count > 0);

            return list.ToArray();
        }

        public async Task<PullRequest> GetPullRequestAsync(int prNumber)
        {
            var response = await CallApiAsync(HttpMethod.Get, $"/pulls/{prNumber}");
            return JsonConvert.DeserializeObject<PullRequest>(response);
        }

        private async Task<string> CallApiAsync<T>(HttpMethod method, string endpoint, T body)
        {
            using (var client = new HttpClient())
            {
                var api = $"{BaseUri}{endpoint}";
                var message = new HttpRequestMessage(method, api);
                message.Headers.Authorization = new AuthenticationHeaderValue("token", AccessToken);
                message.Headers.UserAgent.Add(new ProductInfoHeaderValue(UserAgent, UserAgentVersion));
                
                if (body != null)
                {
                    var content = JsonConvert.SerializeObject(body);
                    message.Content =
                        new StringContent(content, Encoding.UTF8, "application/json");
                }

                var response = await client.SendAsync(message);

                if (!response.IsSuccessStatusCode)
                    throw new Exception(
                        $"Failed to send {message.Method} request to {message.RequestUri} with content ({message.Content}):"
                        + $" Got {response.StatusCode} ({await response.Content.ReadAsStringAsync()})");

                return await response.Content.ReadAsStringAsync();
            }
        }

        public static int? GetBranchId(string origin)
        {
            var branch = origin;

            if (string.IsNullOrEmpty(branch))
                return null;

            if (!branch.StartsWith("refs/pull/") || !branch.EndsWith("/head"))
                return null;

            branch = branch.Substring("refs/pull/".Length, branch.Length - "refs/pull/".Length);
            branch = branch.Substring(0, branch.Length - "/head".Length);

            return int.Parse(branch);
        }

        private async Task<string> CallApiAsync(HttpMethod method, string endpoint)
        {
            return await CallApiAsync<string>(method, endpoint, null);
        }
    }
}
