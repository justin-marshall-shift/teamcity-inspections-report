using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using teamcity_inspections_report.Common;
using teamcity_inspections_report.Common.Git;
using teamcity_inspections_report.Common.GitHub;
using Teamcity_inspections_report.Common.GitHub;
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
        private readonly bool _dryRun;
        private readonly string _output;

        public DerivationChecker(DerivationOptions options)
        {
            _github = new GithubApi(options.GithubToken);
            _teamcityApi = new TeamCityServiceClient(options.TeamCityUrl, options.TeamCityToken);
            _git = new Git(options.Repository);
            _buildId = options.BuildId;
            _scoped = options.IsScoped;
            _derivation = options.Derivation;
            _dryRun = options.DryRun;
            _output = options.Output;
        }

        public async Task RunAsync()
        {
            if (_dryRun)
                Console.WriteLine("== THIS IS A DRY-RUN ==");
            var utcNow = DateTime.UtcNow;
            Console.WriteLine("Fetch remote branches");
            
            if (_scoped)
            {
                await CheckCurrentBranch(utcNow);
            }
            else
            {
                await _git.FetchPrune();
                await CheckAllBranches(utcNow);
            }
        }

        private async Task CheckCurrentBranch(DateTime now)
        {
            Console.WriteLine($"Checking status for build {_buildId}");
            var build = await _teamcityApi.GetTeamCityBuild(_buildId);
            var id = GithubApi.GetBranchId(build.BranchName);
            var buildUrl = await _teamcityApi.GetTeamCityBuildUrl(_buildId, "", false);

            if (!id.HasValue)
            {
                Console.WriteLine($"{build.BranchName} is not a valid PR branch name");
                return;
            }

            var pullRequest = await _github.GetPullRequestAsync(id.Value);

            await CheckDerivationStatus(pullRequest, now, buildUrl);
        }

        private async Task CheckDerivationStatus(PullRequest pullRequest, DateTime now, string buildUrl)
        {
            Console.WriteLine($"Checking status for pull request \"{pullRequest.Title}\"");
            var commit = await _git.GetCommonAncestorWithDevelop(pullRequest.Head.Commit);
            
            if (string.IsNullOrEmpty(commit))
            {
                Console.WriteLine("Could not retrieve ancestor commit");
                return;
            }

            pullRequest.LastDevelopCommit = commit;
            Console.WriteLine($"Common ancestor is commit {commit}");

            var commitDate = await _git.GetCommitDate(commit);
            
            if (!commitDate.HasValue)
            {
                Console.WriteLine("Could not retrieve commit date");
                return;
            }

            pullRequest.LastDevelopMerge = commitDate;
            Console.WriteLine($"This commit dates from {commitDate.Value:f}");

            var isUpToDate = commitDate.Value >= GetTimeLimit(now);

            var status = new StatusCheck
            {
                TargetUrl = buildUrl,
                Context = "Recent merge from develop"
            };
            if (isUpToDate)
            {
                Console.WriteLine($"    \"{pullRequest.Title}\" is up to date");
                status.State = StatusCheckType.success;
                status.Description = "The branch contains a recent version of develop";
            }
            else
            {
                var span = now - commitDate.Value;
                Console.WriteLine($"    \"{pullRequest.Title}\" derived for more than {span.Days} days");
                status.State = StatusCheckType.failure;
                status.Description = $"The branch derived from develop for more than {span.Days} days";
            }

            if(!_dryRun)
                await _github.SetStatusCheckAsync(pullRequest.Head.Commit, status);
        }

        private DateTime GetTimeLimit(DateTime now)
        {
            return now.AddDays(-_derivation);
        }

        private async Task CheckAllBranches(DateTime now)
        {
            var pullRequests = await _github.GetOpenPullRequest();

            Console.WriteLine($"We retrieve {pullRequests.Length} active pull requests from GitHub");
            var buildUrl = await _teamcityApi.GetTeamCityBuildUrl(_buildId, "", false);

            var total = pullRequests.Length;
            var index = 0;

            foreach (var pullRequest in pullRequests)
            {
                index++;
                if (pullRequest.Creation >= GetTimeLimit(now))
                {
                    Console.WriteLine($"=== ({index}/{total}) PR '{pullRequest.Title}' is recent, we will not check it");
                    continue;
                }

                Console.WriteLine($"=== ({index}/{total}) PR '{pullRequest.Title}' was created on {pullRequest.Creation:f}, we will check it");
                await CheckDerivationStatus(pullRequest, now, buildUrl);
            }

            await SaveActiveBranches(pullRequests, now);
        }

        private async Task SaveActiveBranches(PullRequest[] pullRequests, DateTime now)
        {
            await Task.Yield();
            if (!string.IsNullOrEmpty(_output))
            {
                var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ";",
                    ShouldQuote = (s, context) => true,
                    IgnoreBlankLines = true,
                    NewLine = NewLine.CRLF
                };

                var details = pullRequests.Select(p => new BranchInformation
                {
                    Creation = p.Creation.ToUniversalTime().ToString("o"),
                    Id = p.Number,
                    IsWip = p.Title.Contains("[WIP]"),
                    Title = p.Title,
                    Commit = p.LastDevelopCommit,
                    DevMerge = p.LastDevelopMerge?.ToUniversalTime().ToString("o") ?? string.Empty,
                    Derivation = p.LastDevelopMerge.HasValue ? (now - p.LastDevelopMerge.Value).Days : -1
                }).ToArray();

                var path = Path.Combine(_output,$"active_branches_{now.ToLocalTime():yyyy_MM_dd_hhmm}.csv");

                using (var stream = File.OpenWrite(path))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                using (var csvWriter = new CsvWriter(writer, configuration))
                {
                    csvWriter.WriteHeader<BranchInformation>();
                    await csvWriter.NextRecordAsync();
                    await csvWriter.WriteRecordsAsync(details);
                    await csvWriter.FlushAsync();
                }
            }
        }
    }
}
