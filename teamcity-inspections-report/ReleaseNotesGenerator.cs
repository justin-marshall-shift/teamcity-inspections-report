using System.Threading.Tasks;
using teamcity_inspections_report.Common;
using teamcity_inspections_report.Options;

namespace teamcity_inspections_report
{
    public class ReleaseNotesGenerator
    {
        private readonly TeamCityServiceClient _teamCityService;
        private readonly long _buildId;

        public ReleaseNotesGenerator(ReleaseNotesOptions options)
        {
            _buildId = options.BuildId;
            _teamCityService = new TeamCityServiceClient(options.TeamCityUrl, options.TeamCityToken);
        }

        public async Task RunAsync()
        {
            var currentBuild = await _teamCityService.GetTeamCityBuild(_buildId);
            var previousBuild = await _teamCityService.GetTeamCityLastBuildOfBuildType(currentBuild.BuildTypeId);


        }
    }
}
