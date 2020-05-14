using System;
using System.Linq;
using System.Threading.Tasks;
using teamcity_inspections_report.Common;
using teamcity_inspections_report.Options;

namespace teamcity_inspections_report.Validators
{
    public class GithubStatusValidator
    {
        private const string Repository = "shift-technology/shift";
        private const string UserAgent = "Shift-Teamcity-Reporter";
        private const string UserAgentVersion = "1.0";

        private readonly long _buildId;
        private readonly string _gitToken;
        private readonly string _teamcityToken;
        private readonly string _teamcityUrl;
        private readonly string[] _configs;

        public GithubStatusValidator(DeprecatedOptions options)
        {
            _buildId = options.BuildId;
            _gitToken = options.GithubToken;
            _teamcityToken = options.TeamCityToken;
            _teamcityUrl = options.TeamCityUrl;
            _configs = options.Configurations.ToArray();
        }

        public async Task RunAsync()
        {
            var prBuild = await TeamCityHelper.GetTeamCityBuild(_teamcityToken, _teamcityUrl, _buildId);

            var prNumber = GetBranchId(prBuild.BranchName);

            if (!prNumber.HasValue)
            {
                Console.WriteLine("This build is not for a pull request");
                return;
            }

            var github = new GithubApi(UserAgent, UserAgentVersion, _gitToken, Repository);

            foreach (var configs in _configs)
            {
                var parts = configs.Split(':');
                var oldConfiguration = parts[0];
                var newConfiguration = parts[1];

                var oldBuildType = await TeamCityHelper.GetTeamCityBuildType(_teamcityToken, _teamcityUrl, oldConfiguration);
                var newBuildUrl =
                    await TeamCityHelper.GetTeamCityLastBuildUrlOfBuildType(_teamcityToken, _teamcityUrl, newConfiguration);

                await github.SetStatusCheckAsync(prBuild.Revisions.Revision.First().Version, new StatusCheck
                {
                    State = StatusCheckType.Success,
                    TargetUrl = newBuildUrl,
                    Description = "Deprecated - This check has been replaced by a daily build",
                    Context = $"{oldBuildType.Name} ({oldBuildType.Project.Name})"
                });
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
    }
}
