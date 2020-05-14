using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using teamcity_inspections_report.Common;
using teamcity_inspections_report.Duplicates;
using teamcity_inspections_report.Hangout;
using teamcity_inspections_report.Inspection;

namespace teamcity_inspections_report.Reporters
{
    public class InspectionReporter
    {
        private readonly string _currentFilePath;
        private readonly string _webhook;
        private readonly long _buildId;
        private readonly string _teamCityUrl;
        private readonly string _teamCityToken;
        private readonly string _output;

        public InspectionReporter(string currentFilePath, string webhook, long buildId, string teamCityUrl,
            string teamCityToken, string output)
        {
            _currentFilePath = currentFilePath;
            _webhook = webhook;
            _buildId = buildId;
            _teamCityUrl = teamCityUrl;
            _teamCityToken = teamCityToken;
            _output = output;
        }

        public async Task RunAsync()
        {
            await Task.Yield();

            var baseFile = RetrieveBaseFile();
            Console.WriteLine($"Retrieving base file: {baseFile}");

            var baseIssues = new Dictionary<string, Issue>();
            if (File.Exists(baseFile))
            {
                baseIssues = Load(baseFile);
            }
            var baseKeys = baseIssues.Keys.ToHashSet();
            var currentIssues = Load(_currentFilePath);
            var currentKeys = currentIssues.Keys.ToHashSet();
            
            var newIssues = currentIssues.Where(x => !baseKeys.Contains(x.Key)).Select(x => x.Value).ToArray();
            var removedIssues = baseIssues.Where(x => !currentKeys.Contains(x.Key)).Select(x => x.Value).ToArray();

            var nowUtc = DateTime.UtcNow;

            using (var httpClient = new HttpClient())
            {
                foreach (var message in await GetMessages(newIssues, removedIssues, nowUtc, currentIssues.Count))
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

        private async Task<HttpContent[]> GetMessages(Issue[] newIssues, Issue[] removedIssues, DateTime nowUtc, int total)
        {
            var sections = await GetSections(newIssues, removedIssues, total);

            Console.WriteLine("Creating header section of message");
            var card = new HangoutCard
            {
                Header = CardBuilderHelper.GetCardHeader("<b>Inspection daily report</b>", nowUtc, "https://icon-icons.com/icons2/624/PNG/128/Inspection-80_icon-icons.com_57310.png"),
                Sections = sections
            };
            var content = JsonConvert.SerializeObject(new HangoutCardMessage
            {
                Cards = new[] { card }
            });

            Console.WriteLine($"Sending message to Hangout:\r\n{content}");
            return new HttpContent[] { new StringContent(content) };
        }

        private async Task<HangoutCardSection[]> GetSections(Issue[] newIssues, Issue[] removedIssues, int total)
        {
            var hasNew = newIssues.Length > 0;
            var hasLess = removedIssues.Length > 0;

            var hasNewErrors = newIssues.Any(i => i.Severity == Severity.ERROR);
            var hasRemovedErrors = removedIssues.Any(i => i.Severity == Severity.ERROR);

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
                sections.Add(CardBuilderHelper.GetTextParagraphSection(
                    $"+ <b>{newIssues.Length}</b> violation{(newIssues.Length == 1 ? "has" : "s have")} been introduced."));
            }

            if (hasNewErrors)
            {
                var errors = newIssues.Where(i => i.Severity == Severity.ERROR).ToArray();
                Console.WriteLine($"Adding section for the {errors.Length} new errors");
                sections.Add(CardBuilderHelper.GetTextParagraphSection(
                    $"<font color=\"#ff0000\">+ <b>{errors.Length}</b> error{(errors.Length == 1 ? "has" : "s have")} been introduced.</font>"));
            }

            if (hasLess)
            {
                Console.WriteLine($"Adding section for the {removedIssues.Length} removed violations");
                sections.Add(CardBuilderHelper.GetTextParagraphSection(
                    $"- <b>{removedIssues.Length}</b> violation{(removedIssues.Length == 1 ? "has" : "s have")} been removed."));
            }

            if (hasRemovedErrors)
            {
                var errors = removedIssues.Where(i => i.Severity == Severity.ERROR).ToArray();
                Console.WriteLine($"Adding section for the {errors.Length} removed errors");
                sections.Add(CardBuilderHelper.GetTextParagraphSection(
                    $"<font color=\"#00ff00\">- <b>{errors.Length}</b> error{(errors.Length == 1 ? "has" : "s have")} been removed.</font>"));
            }

            sections.Add(await CardBuilderHelper.GetLinkSectionToTeamCityBuild(_teamCityToken, _teamCityUrl, _buildId, "&tab=Inspection"));

            return sections.ToArray();
        }

        private Dictionary<string, Issue> Load(string filePath)
        {
            var baseElement = XElement.Load(filePath);
            var issueTypeNodes = baseElement.Descendants("IssueType");
            var projectNodes = baseElement.Descendants("Project");
            var issueTypes = issueTypeNodes.Select(GetIssueType).ToDictionary(i => i.Id);
            return projectNodes.SelectMany(i => GetIssues(i, issueTypes)).ToDictionary(i => i.Key);
        }

        private static IssueType GetIssueType(XElement issueType)
        {
            return new IssueType
            {
                Id = (string)issueType.Attribute("Id"),
                Category = (string)issueType.Attribute("Category"),
                CategoryId = (string)issueType.Attribute("CategoryId"),
                Description = (string)issueType.Attribute("Description"),
                Severity = Enum.Parse<Severity>((string)issueType.Attribute("Severity")),
                Wiki = (string)issueType.Attribute("WikiUrl")
            };
        }

        private Issue[] GetIssues(XElement project, Dictionary<string, IssueType> issueTypes)
        {
            var projectName = (string)project.Attribute("Name");
            var issueNodes = project.Descendants("Issue").Select(x => GetIssue(projectName, x, issueTypes)).ToArray();
            SetKey(issueNodes);
            return issueNodes;
        }

        private static void SetKey(IEnumerable<Issue> issues)
        {
            var groups = issues.GroupBy(i => i.File);

            foreach (var group in groups)
            {
                var computedKeys = new HashSet<string>();
                foreach (var issue in group)
                {
                    issue.Key = ComputeKey(issue, computedKeys);
                }
            }
        }

        private static string ComputeKey(Issue issue, ISet<string> computedKeys)
        {
            var sb = new StringBuilder();
            sb.Append(issue.File);
            sb.Append('-');
            sb.Append(HashHelper.ComputeHash(issue.Message));

            var key = sb.ToString();

            while (computedKeys.Contains(key))
            {
                key += "#";
            }

            computedKeys.Add(key);
            return key;
        }

        private Issue GetIssue(string projectName, XElement issueNode, IReadOnlyDictionary<string, IssueType> issueTypes)
        {
            var issueSeverity = issueNode.Attribute("Severity");
            var severity = issueSeverity == null ?
                issueTypes[(string) issueNode.Attribute("TypeId")].Severity 
              : Enum.Parse<Severity>((string) issueSeverity);

            return new Issue
            {
                File = (string)issueNode.Attribute("File"),
                Line = issueNode.Attribute("Line") == null ? 0 : (int)issueNode.Attribute("Line"),
                Message = (string)issueNode.Attribute("Message"),
                Offset = GetRange((string)issueNode.Attribute("Offset")),
                Project = projectName,
                TypeId = (string)issueNode.Attribute("TypeId"),
                Severity = severity
            };
        }

        private static Range GetRange(string offset)
        {
            var parts = offset.Split('-', StringSplitOptions.RemoveEmptyEntries);

            return new Range
            {
                End = int.Parse(parts[1]),
                Start = int.Parse(parts[0])
            };
        }

        private string RetrieveBaseFile()
        {
            return Path.Combine(_output, "inspections.xml");
        }
    }
}
