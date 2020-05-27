using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using ToolKit.Common.GitHub;
using ToolKit.Common.TeamCity;
using ToolKit.Options;
using ToolKit.TeamCityService;
using File = System.IO.File;

namespace ToolKit.Monitoring
{
    public class DeepMonitor
    {
        private readonly TeamCityServiceClient _teamCityService;
        private readonly string _folder;
        private readonly GithubApi _github;

        public DeepMonitor(DeepMonitorOptions options)
        {
            _teamCityService = new TeamCityServiceClient(options.Url, options.Token);
            _folder = options.Folder;
            _github = new GithubApi(options.GitHubToken);
        }

        public async Task RunAsync(int period, CancellationToken cancellationToken)
        {
            try
            {
                await LoopAsync(period, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
        }

        private async Task LoopAsync(int period, CancellationToken cancellationToken)
        {
            var delay = TimeSpan.FromMinutes(period);
            var now = DateTime.UtcNow;
            var currentMonitoringTime = now;

            var buildIds = new HashSet<long>();
            var buildDumpIds = new HashSet<long>();

            var branchesToCheck = new HashSet<string>();

            var (queueCsvPath, buildsCsvPath, agentsCsvPath, branchesCsvPath) = GetPaths(currentMonitoringTime);
            var queueOutput = GetAndInitializeWriter<BuildInQueue>(queueCsvPath);
            var buildsOutput = GetAndInitializeWriter<BuildDetails>(buildsCsvPath);
            var agentsOutput = GetAndInitializeWriter<AllAgentsStatus>(agentsCsvPath);
            var branchOutput = GetAndInitializeWriter<BranchStatus>(branchesCsvPath);

            try
            {
                while (true)
                {
                    if (now.Date != currentMonitoringTime.Date)
                    {
                        await RetrieveBranchesStatus(branchOutput, branchesToCheck, now);

                        await FlushAndDisposeOutput(queueOutput);
                        await FlushAndDisposeOutput(buildsOutput);
                        await FlushAndDisposeOutput(agentsOutput);
                        await FlushAndDisposeOutput(branchOutput);
                        buildIds.Clear();
                        buildDumpIds.Clear();
                        branchesToCheck.Clear();
                        (queueCsvPath, buildsCsvPath, agentsCsvPath, branchesCsvPath) = GetPaths(now);
                        queueOutput = GetAndInitializeWriter<BuildInQueue>(queueCsvPath);
                        buildsOutput = GetAndInitializeWriter<BuildDetails>(buildsCsvPath);
                        agentsOutput = GetAndInitializeWriter<AllAgentsStatus>(agentsCsvPath);
                        branchOutput = GetAndInitializeWriter<BranchStatus>(branchesCsvPath);
                        currentMonitoringTime = now;
                    }

                    var agents = await _teamCityService.GetTeamCityAgents(cancellationToken);
                    var queue = await _teamCityService.GetTeamCityQueuedBuilds(cancellationToken);

                    RetrieveBuildsToMonitor(buildIds, buildDumpIds,
                        agents.Agent.Where(a => a.Build != null).Select(a => a.Build),
                        queue.Build);

                    var agentsTask = WriteAgentsAsync(agents, agentsOutput, now);
                    var queueTask = WriteQueueAsync(queue, queueOutput, now);
                    var buildsTask = WriteBuildsAsync(buildIds, buildDumpIds, buildsOutput, branchesToCheck, force: false, cancellationToken: cancellationToken);

                    await Task.WhenAll(queueTask, buildsTask, agentsTask, Task.Delay(delay, cancellationToken));

                    now = DateTime.UtcNow;
                }
            }
            finally
            {
                await WriteBuildsAsync(buildIds, buildDumpIds, buildsOutput, branchesToCheck, force: true, cancellationToken: cancellationToken);
                await FlushAndDisposeOutput(queueOutput);
                await FlushAndDisposeOutput(buildsOutput);
                await FlushAndDisposeOutput(agentsOutput);
                await RetrieveBranchesStatus(branchOutput, branchesToCheck, now);
                await FlushAndDisposeOutput(branchOutput);
            }
        }

        private void RetrieveBuildsToMonitor(HashSet<long> buildIds, HashSet<long> buildDumpIds, params IEnumerable<Build>[] builds)
        {
            foreach (var build in builds.SelectMany(b => b))
            {
                if (build.Id.HasValue && !buildDumpIds.Contains(build.Id.Value))
                    buildIds.Add(build.Id.Value);
            }
        }

        private async Task WriteBuildsAsync(HashSet<long> buildIds, HashSet<long> buildDumpIds,
            (Stream stream, TextWriter writer, CsvWriter csvWriter) output, HashSet<string> branchesToCheck, bool force, CancellationToken cancellationToken)
        {
            var builds = new List<Build>();

            foreach (var buildId in buildIds)
            {
                var build = await _teamCityService.GetTeamCityBuild(buildId, cancellationToken);

                if ((force || !string.IsNullOrEmpty(build.FinishDate)) && !buildDumpIds.Contains(buildId))
                    builds.Add(build);
            }

            // ReSharper disable once StringLiteralTypo
            const string teamCityDateFormat = "yyyyMMddTHHmmsszzz";
            var (_, writer, csvWriter) = output;
            await csvWriter.WriteRecordsAsync(builds.Select(build =>
            {
                buildDumpIds.Add(build.Id ?? throw new NullReferenceException(nameof(build.Id)));
                branchesToCheck.Add(build.BranchName);
                return new BuildDetails
                {
                    Id = build.Id?.ToString(),
                    Type = build.BuildTypeId,
                    Agent = build.Agent?.Name,
                    Branch = build.BranchName,
                    FinishedDate = !string.IsNullOrEmpty(build.FinishDate)
                        ? DateTime.ParseExact(build.FinishDate, teamCityDateFormat, CultureInfo.InvariantCulture,
                            DateTimeStyles.None).ToUniversalTime().ToString("o")
                        : DateTime.MinValue.ToString("o"),
                    QueueDate = !string.IsNullOrEmpty(build.QueuedDate)
                        ? DateTime.ParseExact(build.QueuedDate, teamCityDateFormat, CultureInfo.InvariantCulture,
                            DateTimeStyles.None).ToUniversalTime().ToString("o")
                        : DateTime.MinValue.ToString("o"),
                    StartDate = !string.IsNullOrEmpty(build.StartDate)
                        ? DateTime.ParseExact(build.StartDate, teamCityDateFormat, CultureInfo.InvariantCulture,
                            DateTimeStyles.None).ToUniversalTime().ToString("o")
                        : DateTime.MinValue.ToString("o"),
                    State = build.State,
                    Status = build.Status,
                    Trigger = build.Triggered?.User?.Username,
                    TriggerTime = !string.IsNullOrEmpty(build.Triggered?.Date)
                        ? DateTime.ParseExact(build.Triggered.Date, teamCityDateFormat,
                            CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime().ToString("o")
                        : DateTime.MinValue.ToString("o"),
                    TriggerType = build.Triggered?.Type
                };
            })
            );

            await csvWriter.FlushAsync();
            await writer.FlushAsync();
        }

        private (string queueCsvPath, string buildsCsvPath, string agentsCsvPath, string branchesCsvPath) GetPaths(DateTime time)
        {
            var queueCsvPath = Path.Combine(_folder, $"queue_{time:yyyyMMdd}.csv");
            var buildsCsvPath = Path.Combine(_folder, $"builds_{time:yyyyMMdd}.csv");
            var agentsCsvPath = Path.Combine(_folder, $"agents_{time:yyyyMMdd}.csv");
            var branchesCsvPath = Path.Combine(_folder, $"branches_{time:yyyyMMdd}.csv");
            return (queueCsvPath, buildsCsvPath, agentsCsvPath, branchesCsvPath);
        }

        private async Task WriteQueueAsync(Builds result, (Stream stream, TextWriter writer, CsvWriter csvWriter) output, DateTime now)
        {
            await Task.Yield();
            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = Task.Run(() => Console.WriteLine($"Number of builds {result.Count} at {now}"));
            var builds = result.Build;

            var (_, writer, csvWriter) = output;
            await csvWriter.WriteRecordsAsync(builds.Select(build =>
                new BuildInQueue
                {
                    Timestamp = now.ToString("o"),
                    Id = build.Id?.ToString()
                })
            );
            await csvWriter.FlushAsync();
            await writer.FlushAsync();
        }

        private async Task WriteAgentsAsync(Agents result, (Stream stream, TextWriter writer, CsvWriter csvWriter) output, DateTime now)
        {
            await Task.Yield();

            var agents = result.Agent.ToArray();

            var (_, writer, csvWriter) = output;
            var idleAgents = agents.Where(a => a.Build == null).Select(a => a.Name).ToArray();

            csvWriter.WriteRecord(new AllAgentsStatus
            {
                Disabled = agents.Count(a => a.Enabled == false),
                Total = result.Count ?? 0,
                Idle = (double)idleAgents.Length * 100 / (result.Count ?? 0),
                Timestamp = now.ToString("o"),
                Unauthorized = agents.Count(a => a.Authorized == false),
                IdleAgents = string.Join(",", idleAgents)
            });
            await csvWriter.NextRecordAsync();
            await csvWriter.FlushAsync();
            await writer.FlushAsync();
        }

        public async Task FlushAndDisposeOutput((Stream stream, TextWriter writer, CsvWriter csvWriter) output)
        {
            var (stream, writer, csvWriter) = output;
            if (stream == null || writer == null || csvWriter == null)
                return;

            await csvWriter.FlushAsync();
            await writer.FlushAsync();

            await csvWriter.DisposeAsync();
            writer.Dispose();
            stream.Dispose();
        }

        public (Stream stream, TextWriter writer, CsvWriter csvWriter) GetAndInitializeWriter<T>(string queueCsvPath)
        {
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                ShouldQuote = (s, context) => true,
                IgnoreBlankLines = true,
                NewLine = NewLine.CRLF
            };

            var stream = File.OpenWrite(queueCsvPath);
            var writer = new StreamWriter(stream, Encoding.UTF8);
            var csvWriter = new CsvWriter(writer, configuration);

            csvWriter.WriteHeader<T>();
            csvWriter.NextRecord();

            return (stream, writer, csvWriter);
        }

        public async Task RetrieveBranchesStatus((Stream stream, TextWriter writer, CsvWriter csvWriter) output, HashSet<string> branchesToCheck, DateTime now)
        {
            var (_, writer, csvWriter) = output;

            foreach (var branch in branchesToCheck)
            {
                var id = GetBranchId(branch);
                if (!id.HasValue)
                    continue;

                var pr = await _github.GetPullRequestAsync(id.Value);

                if (pr == null)
                    continue;

                csvWriter.WriteRecord(new BranchStatus
                {
                    Branch = branch,
                    ClosedDate = pr.Cloture.ToUniversalTime().ToString("o"),
                    CreatedDate = pr.Creation.ToUniversalTime().ToString("o"),
                    MergedDate = pr.Merge.ToUniversalTime().ToString("o"),
                    IsWip = pr.Title?.Contains("[WIP]") == true,
                    State = pr.State,
                    StatusDate = now.Date.ToUniversalTime().ToString("o"),
                    Title = pr.Title,
                    Url = pr.Url
                });

                await csvWriter.NextRecordAsync();
                await csvWriter.FlushAsync();
                await writer.FlushAsync();
            }
        }

        public int? GetBranchId(string origin)
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
