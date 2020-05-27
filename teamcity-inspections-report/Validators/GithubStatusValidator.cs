using System;
using System.Linq;
using System.Threading.Tasks;
using ToolKit.Common.GitHub;
using ToolKit.Common.TeamCity;
using ToolKit.Options;

namespace ToolKit.Validators
{
    public class GithubStatusValidator
    {
        private readonly long _buildId;
        private readonly string _gitToken;
        private readonly TeamCityServiceClient _teamcityService;
        private readonly string[] _configs;

        public GithubStatusValidator(DeprecatedOptions options)
        {
            _buildId = options.BuildId;
            _gitToken = options.GithubToken;
            _configs = options.Configurations.ToArray();
            _teamcityService = new TeamCityServiceClient(options.TeamCityUrl, options.TeamCityToken);
        }

        public async Task RunAsync()
        {
            var prBuild = await _teamcityService.GetTeamCityBuild(_buildId);

            var prNumber = GetBranchId(prBuild.BranchName);

            if (!prNumber.HasValue)
            {
                Console.WriteLine("This build is not for a pull request");
                return;
            }

            var github = new GithubApi(_gitToken);

            foreach (var configs in _configs)
            {
                var parts = configs.Split(':');
                var oldConfiguration = parts[0];
                var newConfiguration = parts[1];

                var oldBuildType = await _teamcityService.GetTeamCityBuildType(oldConfiguration);
                var newBuildUrl = await _teamcityService.GetTeamCityLastBuildUrlOfBuildType(newConfiguration);

                await github.SetStatusCheckAsync(prBuild.Revisions.Revision.First().Version, new StatusCheck
                {
                    State = StatusCheckType.success,
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
