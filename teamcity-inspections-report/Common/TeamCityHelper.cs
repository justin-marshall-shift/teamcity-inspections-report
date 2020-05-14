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
        public static async Task<string> GetTeamCityBuildUrl(string token, string url, long buildId, string tab, bool withRedirect = true)
        {
            var build = await GetTeamCityBuild(token, url, buildId);

            return TeamCityBuildUrl(tab, withRedirect, build);
        }

        private static string TeamCityBuildUrl(string tab, bool withRedirect, Build build)
        {
            var uriBuilder = new UriBuilder(build.WebUrl) {Scheme = Uri.UriSchemeHttp};

            var buildUrl = uriBuilder.ToString().Replace(":80", string.Empty);
            Console.WriteLine($"Retrieving build url: {buildUrl}");
            if (!withRedirect) return buildUrl;

            var encode = HttpUtility.UrlEncode(buildUrl + tab);
            return $"https://httpbin.org/redirect-to?url={encode}";
        }

        public static async Task<Build> GetTeamCityBuild(string token, string url, long buildId)
        {
            var client = GetTeamCityClient(token, url);
            return await client.ServeBuildAsync($"id:{buildId}", null);
        }

        private static Client GetTeamCityClient(string token, string url)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            httpClient.BaseAddress = new Uri(url);
            var client = new Client(httpClient) {BaseUrl = url};
            return client;
        }

        public static async Task<BuildType> GetTeamCityBuildType(string token, string url, string buildTypeId)
        {
            var client = GetTeamCityClient(token, url);
            return await client.ServeBuildTypeXMLAsync($"id:{buildTypeId}", null);
        }

        public static async Task<Build> GetTeamCityLastBuildOfBuildType(string token, string url, string buildTypeId)
        {
            var client = GetTeamCityClient(token, url);
            return await client.ServeBuildAsync($"buildType:{buildTypeId},status:success,count:1", null);
        }

        public static async Task<string> GetTeamCityLastBuildUrlOfBuildType(string token, string url, string buildTypeId)
        {
            var build = await GetTeamCityLastBuildOfBuildType(token, url, buildTypeId);

            return TeamCityBuildUrl(string.Empty, false, build);
        }
    }
}
