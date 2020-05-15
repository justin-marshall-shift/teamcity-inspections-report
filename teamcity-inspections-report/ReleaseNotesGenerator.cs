using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using teamcity_inspections_report.Common;
using teamcity_inspections_report.Options;

namespace teamcity_inspections_report
{
    public class ReleaseNotesGenerator
    {
        private readonly TeamCityServiceClient _teamCityService;
        private readonly long _buildId;
        private readonly string _audit;
        private readonly JiraService _jira;
        private readonly GithubApi _github;

        public ReleaseNotesGenerator(ReleaseNotesOptions options)
        {
            _buildId = options.BuildId;
            _teamCityService = new TeamCityServiceClient(options.TeamCityUrl, options.TeamCityToken);
            _audit = options.Audit;
            _jira = new JiraService(options.Login, options.Password);
            _github = new GithubApi(options.GithubToken);
        }

        public async Task RunAsync()
        {
            var utcNow = DateTime.UtcNow;

            var currentBuild = await _teamCityService.GetTeamCityBuild(_buildId);
            var previousBuild = await _teamCityService.GetTeamCityLastBuildOfBuildType(currentBuild.BuildTypeId);

            var baseCommit = previousBuild.Revisions.Revision.First().Version;
            var headCommit = currentBuild.Revisions.Revision.First().Version;

            await GenerateReleaseNotes(baseCommit, headCommit);

            using (var auditFile = File.Open(_audit, FileMode.Append, FileAccess.Write))
            using (var auditWriter = new StreamWriter(auditFile))
            {
                await auditWriter.WriteLineAsync($"{headCommit} - {utcNow:G}");
                await auditWriter.FlushAsync();
            }
        }

        private async Task GenerateReleaseNotes(string baseCommit, string headCommit)
        {
            throw new NotImplementedException();
        }
    }
}
