using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using teamcity_inspections_report.TeamCityService;

namespace teamcity_inspections_report.Common
{
    public class TeamCityServiceClient
    {
        private readonly string _teamCityUrl;
        private readonly string _teamCityToken;

        public TeamCityServiceClient(string teamCityUrl, string teamCityToken)
        {
            _teamCityUrl = teamCityUrl;
            _teamCityToken = teamCityToken;
        }

        public async Task<string> GetTeamCityBuildUrl(long buildId, string tab, bool withRedirect = true)
        {
            var build = await GetTeamCityBuild(buildId);

            return TeamCityBuildUrl(tab, withRedirect, build);
        }

        private static string TeamCityBuildUrl(string tab, bool withRedirect, Build build)
        {
            var uriBuilder = new UriBuilder(build.WebUrl) { Scheme = Uri.UriSchemeHttp };

            var buildUrl = uriBuilder.ToString().Replace(":80", string.Empty);
            Console.WriteLine($"Retrieving build url: {buildUrl}");
            if (!withRedirect) return buildUrl;

            var encode = HttpUtility.UrlEncode(buildUrl + tab);
            return $"https://httpbin.org/redirect-to?url={encode}";
        }

        public async Task<Build> GetTeamCityBuild(long buildId)
        {
            var client = GetTeamCityClient();
            return await client.ServeBuildAsync($"id:{buildId}", null);
        }

        private Client GetTeamCityClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _teamCityToken);
            httpClient.BaseAddress = new Uri(_teamCityUrl);
            var client = new Client(httpClient) { BaseUrl = _teamCityUrl };
            return client;
        }

        public async Task<BuildType> GetTeamCityBuildType(string buildTypeId)
        {
            var client = GetTeamCityClient();
            return await client.ServeBuildTypeXMLAsync($"id:{buildTypeId}", null);
        }

        public async Task<Build> GetTeamCityLastBuildOfBuildType(string buildTypeId)
        {
            var client = GetTeamCityClient();
            return await client.ServeBuildAsync($"buildType:{buildTypeId},status:success,state:finished,count:1", null);
        }

        public async Task<(string baseCommit, string headCommit)> ComputeCommitRange(long buildId)
        {
            var currentBuild = await GetTeamCityBuild(buildId);
            var previousBuild = await GetTeamCityLastBuildOfBuildType(currentBuild.BuildTypeId);

            var baseCommit = previousBuild.Revisions.Revision.First().Version;
            var headCommit = currentBuild.Revisions.Revision.First().Version;

            return (baseCommit, headCommit);
        }

        public async Task<string> GetTeamCityLastBuildUrlOfBuildType(string buildTypeId)
        {
            var build = await GetTeamCityLastBuildOfBuildType(buildTypeId);

            return TeamCityBuildUrl(string.Empty, false, build);
        }
    }
}
