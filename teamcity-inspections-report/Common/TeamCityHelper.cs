using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using teamcity_inspections_report.TeamCityService;

namespace teamcity_inspections_report.Common
{
    public static class TeamCityHelper
    {
        public static async Task<string> GetTeamCityBuildUrl(string token, string url, long buildId, string tab)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            httpClient.BaseAddress = new Uri(url);
            var client = new Client(httpClient) { BaseUrl = url };
            var build = await client.ServeBuildAsync($"id:{buildId}", null);

            var uriBuilder = new UriBuilder(build.WebUrl) { Scheme = Uri.UriSchemeHttp };

            var buildUrl = uriBuilder.ToString().Replace(":80", string.Empty);
            Console.WriteLine($"Retrieving build url: {buildUrl}");
            var encode = HttpUtility.UrlEncode(buildUrl + tab);
            return $"https://httpbin.org/redirect-to?url={encode}";
        }
    }
}
