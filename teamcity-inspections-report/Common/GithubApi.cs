﻿using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace teamcity_inspections_report.Common
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
        private const string GithubApiUri = "https://api.github.com";

        static GithubApi()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public GithubApi(string userAgent, string userAgentVersion, string accessToken, string repository)
        {
            UserAgent = userAgent;
            UserAgentVersion = userAgentVersion;
            AccessToken = accessToken;
            Repository = repository;
        }

        private string UserAgent { get; }
        private string UserAgentVersion { get; }
        private string AccessToken { get; }
        private string Repository { get; }
        private string BaseUri => $"{GithubApiUri}/repos/{Repository}";

        public async Task SetStatusCheckAsync(string commitHash, StatusCheck statusCheck)
        {
            await CallApiAsync(HttpMethod.Post, $"/statuses/{commitHash}", statusCheck);
        }

        private async Task<string> CallApiAsync<T>(HttpMethod method, string endpoint, T body)
        {
            using (var client = new HttpClient())
            {
                var message = new HttpRequestMessage(method, $"{BaseUri}{endpoint}");
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

        private async Task<string> CallApiAsync(HttpMethod method, string endpoint)
        {
            return await CallApiAsync<string>(method, endpoint, null);
        }
    }
}
