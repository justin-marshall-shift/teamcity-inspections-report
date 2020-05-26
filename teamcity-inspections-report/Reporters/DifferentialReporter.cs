using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using teamcity_inspections_report.Common;
using teamcity_inspections_report.Common.Git;
using teamcity_inspections_report.Duplicates;
using teamcity_inspections_report.Hangout;
using teamcity_inspections_report.Options;
using File = System.IO.File;

namespace teamcity_inspections_report.Reporters
{
    public class DifferentialReporter
    {
        private readonly string _currentFilePath;
        private readonly string _webhook;
        private readonly long _buildId;
        private readonly string _output;
        private readonly string _gitPath;
        private readonly TeamCityServiceClient _teamcityService;

        private readonly Dictionary<int,string> _ranks = new Dictionary<int, string>
        {
            {1, "https://icon-icons.com/icons2/847/PNG/128/olympic_medal_gold_icon-icons.com_67221.png"},
            {2, "https://icon-icons.com/icons2/847/PNG/128/olympic_medal_silver_icon-icons.com_67220.png"},
            {3, "https://icon-icons.com/icons2/847/PNG/128/olympic_medal_bronze_icon-icons.com_67222.png"}
        };

        public DifferentialReporter(DifferentialOptions options, string file)
        {
            _currentFilePath = file;
            _teamcityService = new TeamCityServiceClient(options.TeamCityUrl, options.TeamCityToken);
            _webhook = options.Webhook;
            _buildId = options.BuildId;
            _output = options.Output;
            _gitPath = options.Git;
        }

        public async Task RunAsync()
        {
            await Task.Yield();
            var baseFile = RetrieveBaseFile();
            Console.WriteLine($"Retrieving base file: {baseFile}");

            var comparer = new DuplicateComparator(baseFile, _currentFilePath);
            
            var nowUtc = DateTime.UtcNow;

            using (var httpClient = new HttpClient())
            {
                foreach (var message in await GetMessages(comparer, nowUtc))
                {
                    Console.WriteLine($"Webhook: {_webhook}");
                    var response = await httpClient.PostAsync(_webhook, message);

                    Console.WriteLine($"Response: {response.StatusCode.ToString()}");

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine(await response.Content.ReadAsStringAsync());
                        return;
                    }
                }
            }

            if (File.Exists(baseFile))
            {
                var archive = Path.Combine(_output,
                    $"duplicate-report-{nowUtc.AddDays(-1).ToLocalTime():yyyy_MM_dd_hhmm}.xml");
                var baseFileInfo = new FileInfo(baseFile);
                baseFileInfo.CopyTo(archive, true);
                Console.WriteLine($"Backup of base file to {archive}");
            }

            var fileInfo = new FileInfo(_currentFilePath);
            fileInfo.CopyTo(baseFile, true);
            Console.WriteLine("Copy of new base file");
        }

        private string RetrieveBaseFile()
        {
            return Path.Combine(_output, "duplicate_report.xml");
        }

        private async Task<HangoutCard> GetReportCard(DuplicateComparator comparator, DateTime nowUtc)
        {
            var sections = await GetSections(comparator);

            return new HangoutCard
            {
                Header = CardBuilderHelper.GetCardHeader("<b>Duplicate daily report</b>", nowUtc, "https://icon-icons.com/icons2/1278/PNG/128/1497562286-gemini-zodiac-sign_85087.png"),
                Sections = sections
            };
        }

        private async Task<HttpContent[]> GetMessages(DuplicateComparator comparator, DateTime nowUtc)
        {
            Console.WriteLine("Creating header section of message");
            var card = await GetReportCard(comparator, nowUtc);
            var card2 = await GetLeaderBoardCard(comparator);

            var cards = new List<HangoutCard> {card};
            if (card2 != null)
                cards.Add(card2);

            var content = JsonConvert.SerializeObject(new HangoutCardMessage
            {
                Cards = cards.ToArray()
            }, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            });

            Console.WriteLine($"Sending message to Hangout:\r\n{content}");
            return new HttpContent[] { new StringContent(content) };
        }

        private async Task<HangoutCard> GetLeaderBoardCard(DuplicateComparator comparator)
        {
            try
            {
                var (_, removedDuplicates, _) = comparator.GetComparison();

                var blamer = new GitBlamer(_gitPath);

                var (baseCommit, headCommit) = await _teamcityService.ComputeCommitRange(_buildId);

                var contributors = await blamer.GetRemovalContributors(baseCommit, headCommit, removedDuplicates, 3, file => file);

                var sections = new List<HangoutCardSection>();
                var rank = 1;
                foreach (var contributor in contributors)
                {
                    sections.Add(CardBuilderHelper.GetKeyValueSection("Duplication has been removed by", contributor.Name, $"for a cost of {contributor.Contributions.Sum(f => f.Score)}", _ranks[rank]));
                    rank++;
                }

                return new HangoutCard
                {
                    Header = CardBuilderHelper.GetCardHeader("Podium","Who removed duplication", "https://icon-icons.com/icons2/9/PNG/128/podium_1511.png"),
                    Sections = sections.ToArray()
                };
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to retrieve best contributors");
            }
            return null;
        }

        private async Task<HangoutCardSection[]> GetSections(DuplicateComparator comparator)
        {
            var (newDuplicates, removedDuplicates, currentDuplicates) = comparator.GetComparison();

            var hasNew = newDuplicates.Length > 0;
            var hasLess = removedDuplicates.Length > 0;

            Console.WriteLine($"Creating section of message for all {currentDuplicates.Length} duplications");
            var sections = new List<HangoutCardSection>
            {
                CardBuilderHelper.GetTextParagraphSection($"The total of duplications in our code base is <b>{currentDuplicates.Length}</b>.")
            };

            if (!hasNew && !hasLess)
            {
                Console.WriteLine("No change in duplications");
                sections.Add(CardBuilderHelper.GetTextParagraphSection("No change was found during the inspection"));
            }

            if (hasNew)
            {
                Console.WriteLine($"Adding section for the {newDuplicates.Length} new duplicates");
                sections.Add(CardBuilderHelper.GetTextParagraphSection(
                    $"+ <b>{newDuplicates.Length}</b> duplication{(newDuplicates.Length == 1 ? "has" : "s have")} been introduced."));
            }

            if (hasLess)
            {
                Console.WriteLine($"Adding section for the {removedDuplicates.Length} removed duplicates");
                sections.Add(CardBuilderHelper.GetTextParagraphSection(
                    $"- <b>{removedDuplicates.Length}</b> duplication{(removedDuplicates.Length == 1 ? "has" : "s have")} been removed."));
            }

            var url = await _teamcityService.GetTeamCityBuildUrl(_buildId, "&tab=Duplicator");
            sections.Add(await CardBuilderHelper.GetLinkSectionToUrl(url, "Go to TeamCity build"));

            return sections.ToArray();
        }
    }
}
