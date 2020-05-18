using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using teamcity_inspections_report.Common;
using teamcity_inspections_report.Options;

namespace teamcity_inspections_report.Reporters
{
    public class ReleaseNotesMetadataGenerator
    {
        private readonly TeamCityServiceClient _teamCityService;
        private readonly long _buildId;
        private readonly string _path;

        public ReleaseNotesMetadataGenerator(ReleaseNotesMetadataOptions options)
        {
            _teamCityService = new TeamCityServiceClient(options.TeamCityUrl, options.TeamCityToken);
            _buildId = options.BuildId;
            _path = options.Metadata;
        }

        public async Task RunAsync()
        {
            var currentBuild = await _teamCityService.GetTeamCityBuild(_buildId);
            var previousBuild = await _teamCityService.GetTeamCityLastBuildOfBuildType(currentBuild.BuildTypeId);

            var baseCommit = previousBuild.Revisions.Revision.First().Version;
            var headCommit = currentBuild.Revisions.Revision.First().Version;

            var metadata = new MetadataReleaseNotes
            {
                BaseCommit = baseCommit,
                HeadCommit = headCommit
            };

            var content = JsonConvert.SerializeObject(metadata);

            if (File.Exists(_path)) File.Delete(_path);

            using (var file = File.OpenWrite(_path))
            using (var writer = new StreamWriter(file))
            {
                await writer.WriteAsync(content);
                await writer.FlushAsync();
            }
        }
    }
}
