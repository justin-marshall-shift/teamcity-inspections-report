using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using teamcity_inspections_report.Common;
using teamcity_inspections_report.Common.Git;
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
        private readonly SoftwareQualityMailNotifier _mailNotifier;
        private readonly JiraService _jira;

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
            _mailNotifier = new SoftwareQualityMailNotifier(options.Login, options.Password);

            _jira = new JiraService(options.JiraLogin, options.JiraPassword);
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
                    $"inspections-{nowUtc.AddDays(-1).ToLocalTime():yyyy_MM_dd_hhmm}.xml");
                var baseFileInfo = new FileInfo(baseFile);
                baseFileInfo.CopyTo(archive, true);
                Console.WriteLine($"Backup of base file to {archive}");
            }

            var fileInfo = new FileInfo(_currentFilePath);
            fileInfo.CopyTo(baseFile, true);
            Console.WriteLine("Copy of new base file");

            await ManageErrorsAndThreshold(comparer);
        }

        private async Task ManageErrorsAndThreshold(InspectionsComparator comparer)
        {
            var taskMail = SendErrorsAndThresholdThroughMail(comparer);
            var taskJira = CreateJiraTasks(comparer);

            await Task.WhenAll(taskMail, taskJira);
        }

        private async Task CreateJiraTasks(InspectionsComparator comparer)
        {
            await Task.Yield();
            if (!_jira.IsSetUp || await _jira.Noop())
            {
                Console.WriteLine("Jira access is not correctly set up. This step will be skip.");
                return;
            }

            Console.WriteLine("Jira tasks creation [WIP]");

            try
            {
                
                Console.WriteLine("End of Jira tasks creation");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to create tasks. Reason: {e.Message}");
            }
        }

        private async Task SendErrorsAndThresholdThroughMail(InspectionsComparator comparer)
        {
            if (!_mailNotifier.IsSetUp)
            {
                Console.WriteLine("The Software Quality mail notifier is not set up");
                return;
            }

            var (newIssues, _, _) = comparer.GetComparison();
            var newErrors = newIssues.Where(i => i.Severity == Severity.ERROR).ToArray();

            var blamer = new GitBlamer(_gitPath);
            var (_, headCommit) = await _teamcityService.ComputeCommitRange(_buildId);
            var contributors = await blamer.GetNewContributors(headCommit, newErrors, newErrors.Length, ComputePathToRepo);

            foreach (var contributor in contributors)
            {
                try
                {
                    Console.WriteLine($"Sending mail to {contributor.Name} for errors in inspection");
                    var body = await GetBody(contributor.Contributions, contributor.Name);
                    await _mailNotifier.SendMail("Errors in daily inspection", contributor.Mail, contributor.Name, body);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to send mail to {contributor.Name}: {e.Message}");
                }
            }
        }

        private async Task<string> GetBody(HashSet<CodeFragment> contributions, string name)
        {
            var files = contributions.Select(f => f.Path).Distinct();

            var url = await _teamcityService.GetTeamCityBuildUrl(_buildId, "&tab=Inspection", false);

            var body =
                $@"<body style=""margin: 0; padding: 0;"">
 <table border=""1"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
  <tr>
   <td>
    Hello {name},

    New errors have been introduced in the last <a href=""{url}"">daily inspection</a>.
Can you have a look please?
It seems you contributed to the following file(s):
{string.Join("\r\n", files)}

If you received this mail by error, please <a href=""mailto:justin.marshall@shift-technology.com,christophe.guilhou@shift-technology.com&subject=Bad attribution"">notify us</a>.

Thank you,
Best regards,

Shift Quality Team
     </td>
  </tr>
 </table>
</body>";

            return body;
        }

        private async Task<HangoutCard> GetLeaderBoardCard(InspectionsComparator comparator)
        {
            try
            {
                var (_, removedIssues, _) = comparator.GetComparison();

                var blamer = new GitBlamer(_gitPath);

                var (baseCommit, headCommit) = await _teamcityService.ComputeCommitRange(_buildId);

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
