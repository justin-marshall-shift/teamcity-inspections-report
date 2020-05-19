using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using teamcity_inspections_report.Common;
using teamcity_inspections_report.Hangout;
using teamcity_inspections_report.Inspection;
using teamcity_inspections_report.Options;

namespace teamcity_inspections_report.Reporters
{
    public class InspectionReporter
    {
        private readonly string _currentFilePath;
        private readonly string _webhook;
        private readonly long _buildId;
        private readonly TeamCityServiceClient _teamcityService;
        private readonly string _output;
        private readonly string _threshold;
        private readonly string _gitPath;
        private readonly string _relativeSolutionPath;

        private readonly Dictionary<int, string> _ranks = new Dictionary<int, string>
        {
            {1, "https://icon-icons.com/icons2/847/PNG/128/olympic_medal_gold_icon-icons.com_67221.png"},
            {2, "https://icon-icons.com/icons2/847/PNG/128/olympic_medal_silver_icon-icons.com_67220.png"},
            {3, "https://icon-icons.com/icons2/847/PNG/128/olympic_medal_bronze_icon-icons.com_67222.png"}
        };

        public InspectionReporter(InspectionOptions options, string file)
        {
            _currentFilePath = file;
            _webhook = options.Webhook;
            _buildId = options.BuildId;
            _output = options.Output;
            _threshold = options.Threshold;
            _teamcityService = new TeamCityServiceClient(options.TeamCityUrl, options.TeamCityToken);
            _gitPath = options.Git;
            _relativeSolutionPath = options.Solution;
        }

        public async Task RunAsync()
        {
            await Task.Yield();

            var baseFile = RetrieveBaseFile();
            Console.WriteLine($"Retrieving base file: {baseFile}");

            if (!File.Exists(_threshold))
            {
                Console.WriteLine("No threshold file, we will skip the check by project");
            }

            var comparer = new InspectionsComparator(baseFile, _currentFilePath, _threshold);

            var nowUtc = DateTime.UtcNow;

            using (var httpClient = new HttpClient())
            {
                foreach (var message in await GetMessages(comparer, nowUtc))
                {
                    Console.WriteLine($"Webhook: {_webhook}");
                    var response = await httpClient.PostAsync(_webhook, message);

                    Console.WriteLine($"Response: {response.StatusCode.ToString()}");

                    if (response.IsSuccessStatusCode) continue;

                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                    return;
                }
            }

            if (File.Exists(baseFile))
            {
                var archive = Path.Combine(_output,
                    $"inspections-{nowUtc.AddDays(-1).ToLocalTime():yyyy_MM_dd}.xml");
                var baseFileInfo = new FileInfo(baseFile);
                baseFileInfo.CopyTo(archive, true);
                Console.WriteLine($"Backup of base file to {archive}");
            }

            var fileInfo = new FileInfo(_currentFilePath);
            fileInfo.CopyTo(baseFile, true);
            Console.WriteLine("Copy of new base file");
        }

        private async Task<HangoutCard> GetLeaderBoardCard(InspectionsComparator comparator)
        {
            try
            {
                var (_, removedIssues, _) = comparator.GetComparison();

                var blamer = new GitBlamer(_gitPath);

                var currentBuild = await _teamcityService.GetTeamCityBuild(_buildId);
                var previousBuild = await _teamcityService.GetTeamCityLastBuildOfBuildType(currentBuild.BuildTypeId);

                var baseCommit = previousBuild.Revisions.Revision.First().Version;
                var headCommit = currentBuild.Revisions.Revision.First().Version;

                var contributors = await blamer.GetRemovalContributors(baseCommit, headCommit, removedIssues, 3, ComputePathToRepo);

                var sections = new List<HangoutCardSection>();
                var rank = 1;
                foreach (var contributor in contributors)
                {
                    sections.Add(CardBuilderHelper.GetKeyValueSection("Violations have been removed by", contributor.Name, $"for a cost of {contributor.Contributions.Sum(f => f.Score)}", _ranks[rank]));
                    rank++;
                }

                return new HangoutCard
                {
                    Header = CardBuilderHelper.GetCardHeader("Podium", "Who removed violations", "https://icon-icons.com/icons2/9/PNG/128/podium_1511.png"),
                    Sections = sections.ToArray()
                };
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to retrieve best contributors");
            }
            return null;
        }

        private string ComputePathToRepo(string relativePath)
        {
            if (string.IsNullOrEmpty(_relativeSolutionPath))
                return relativePath;

            var solutionFolder = Path.GetDirectoryName(_relativeSolutionPath);

            var path = Path.GetFullPath(Path.Combine(_gitPath, solutionFolder, relativePath));

            return path.Replace(_gitPath, string.Empty).Trim('/', '\\');
        }

        private async Task<HttpContent[]> GetMessages(InspectionsComparator comparer, DateTime nowUtc)
        {
            var (newIssues, removedIssues, currentIssues) = comparer.GetComparison();
            var sections = await GetSections(newIssues, removedIssues, currentIssues, comparer);

            Console.WriteLine("Creating header section of message");
            var card = new HangoutCard
            {
                Header = CardBuilderHelper.GetCardHeader("<b>Inspection daily report</b>", nowUtc, "https://icon-icons.com/icons2/624/PNG/128/Inspection-80_icon-icons.com_57310.png"),
                Sections = sections
            };

            var card2 = await GetLeaderBoardCard(comparer);

            var cards = new List<HangoutCard> { card };
            if (card2 != null)
                cards.Add(card2);

            var content = JsonConvert.SerializeObject(new HangoutCardMessage
            {
                Cards = cards.ToArray()
            });

            Console.WriteLine($"Sending message to Hangout:\r\n{content}");
            return new HttpContent[] { new StringContent(content) };
        }

        private async Task<HangoutCardSection[]> GetSections(Issue[] newIssues, Issue[] removedIssues, Issue[] currentIssues, InspectionsComparator comparer)
        {
            var total = currentIssues.Length;
            var hasNew = newIssues.Length > 0;
            var hasLess = removedIssues.Length > 0;

            var countOfErrors = newIssues.Count(i => i.Severity == Severity.ERROR);
            var countOfRemovedErrors = removedIssues.Count(i => i.Severity == Severity.ERROR);

            Console.WriteLine($"Creating section of message for all {total} violations");
            var sections = new List<HangoutCardSection>
            {
                CardBuilderHelper.GetTextParagraphSection($"The total of violations in our code base is <b>{total}</b>.")
            };

            if (!hasNew && !hasLess)
            {
                Console.WriteLine("No change in inspection");
                sections.Add(CardBuilderHelper.GetTextParagraphSection("No change was found during the inspection"));
            }

            if (hasNew)
            {
                Console.WriteLine($"Adding section for the {newIssues.Length} new violations");
                var message =
                    $"+ <b>{newIssues.Length}</b> violation{(newIssues.Length == 1 ? " has" : "s have")} been introduced.";

                if (countOfErrors > 0)
                {
                    message += $"\r\n<font color=\"#ff0000\">(of which <b>{countOfErrors}</b> {(countOfErrors > 1 ? "are errors" : "is an error")})</font>";
                }

                sections.Add(CardBuilderHelper.GetTextParagraphSection(message));
            }

            if (hasLess)
            {
                Console.WriteLine($"Adding section for the {removedIssues.Length} removed violations");
                var message =
                    $"- <b>{removedIssues.Length}</b> violation{(removedIssues.Length == 1 ? " has" : "s have")} been removed.";

                if (countOfRemovedErrors > 0)
                {
                    message += $"\r\n<font color=\"#00ff00\">(of which <b>{countOfRemovedErrors}</b> {(countOfRemovedErrors > 1 ? "were errors" : "was an error")})</font>";
                }

                sections.Add(CardBuilderHelper.GetTextParagraphSection(message));
            }

            var (numberOfErrors, failedProjects) = comparer.EnforceNumberOfErrorsAndNumberOfViolationsByProject(currentIssues);
            if (numberOfErrors > 0)
            {
                sections.Add(CardBuilderHelper.GetKeyValueSection("Error(s)", $"{numberOfErrors} error{(numberOfErrors > 1 ? "s were" : " was")} detected by the inspection.", string.Empty, "https://icon-icons.com/icons2/1380/PNG/32/vcsconflicting_93497.png"));
            }

            foreach (var failedProject in failedProjects)
            {

                sections.Add(CardBuilderHelper.GetKeyValueSection("Threshold was reached by", failedProject.Project, $"{failedProject.Count} violations", "https://icon-icons.com/icons2/1024/PNG/32/warning_256_icon-icons.com_76006.png"));
            }

            var url = await _teamcityService.GetTeamCityBuildUrl(_buildId, "&tab=Inspection");
            sections.Add(await CardBuilderHelper.GetLinkSectionToUrl(url, "Go to TeamCity build"));

            return sections.ToArray();
        }

        private string RetrieveBaseFile()
        {
            return Path.Combine(_output, "inspections.xml");
        }
    }
}
