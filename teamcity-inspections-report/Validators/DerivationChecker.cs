using System;
using System.Linq;
using System.Threading.Tasks;
using teamcity_inspections_report.Common;
using teamcity_inspections_report.Common.Git;
using teamcity_inspections_report.Common.GitHub;
using teamcity_inspections_report.Options;

namespace teamcity_inspections_report.Validators
{
    public class DerivationChecker
    {
        private readonly GithubApi _github;
        private readonly TeamCityServiceClient _teamcityApi;
        private readonly Git _git;
        private readonly long _buildId;
        private readonly bool _scoped;
        private readonly int _derivation;

        public DerivationChecker(DerivationOptions options)
        {
            _github = new GithubApi(options.GithubToken);
            _teamcityApi = new TeamCityServiceClient(options.TeamCityUrl, options.TeamCityToken);
            _git = new Git(options.Repository);
            _buildId = options.BuildId;
            _scoped = options.IsScoped;
            _derivation = options.Derivation;
        }

        public async Task RunAsync()
        {
            var utcNow = DateTime.UtcNow;
            if (_scoped)
            {
                await CheckCurrentBranch(utcNow);
            }
            else
            {
                await CheckAllBranches(utcNow);
            }
        }

        private async Task CheckCurrentBranch(DateTime now)
        {
            Console.WriteLine($"Checking status for build {_buildId}");
            var build = await _teamcityApi.GetTeamCityBuild(_buildId);
            var id = GithubApi.GetBranchId(build.BranchName);

            if (!id.HasValue)
            {
                Console.WriteLine($"{build.BranchName} is not a valid PR branch name");
                return;
            }

            var pullRequest = await _github.GetPullRequestAsync(id.Value);

            await CheckDerivationStatus(pullRequest, now);
        }

        private async Task CheckDerivationStatus(PullRequest pullRequest, DateTime now)
        {
            Console.WriteLine($"Checking status for pull request \"{pullRequest.Title}\"");
            var commit = await _git.GetCommonAncestorWithDevelop(pullRequest.Head.Reference);
            var commitInformation = await _git.Log(commit);

            var isUpToDate = commitInformation.Date >= GetTimeLimit(now);

            var status = new StatusCheck
            {
                TargetUrl = await _teamcityApi.GetTeamCityBuildUrl(_buildId, "", false),
                Context = "Is latest integration of main branch recent (develop)"
            };
            if (isUpToDate)
            {
                Console.WriteLine($"    \"{pullRequest.Title}\" is up to date");
                status.State = StatusCheckType.success;
                status.Description = "The branch contains a recent version of develop";
            }
            else
            {
                var span = now - commitInformation.Date;
                Console.WriteLine($"    \"{pullRequest.Title}\" derived for more than {span.Days} days");
                status.State = StatusCheckType.failure;
                status.Description = $"The branch derived from develop for more than {span.Days} days";
            }

            await _github.SetStatusCheckAsync(pullRequest.Head.Commit, status);
        }

        private DateTime GetTimeLimit(DateTime now)
        {
            return now.AddDays(-_derivation);
        }

        private async Task CheckAllBranches(DateTime now)
        {
            var pullRequests = await _github.GetOpenPullRequest();

            await _git.FetchPrune();

            foreach (var pullRequest in pullRequests.Where(p => p.Creation <= GetTimeLimit(now)))
            {
                await CheckDerivationStatus(pullRequest, now);
            }
        }
    }
}
