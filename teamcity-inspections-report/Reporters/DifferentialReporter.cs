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
        private readonly TeamCityServiceClient _teamcityService;

        public DifferentialReporter(DifferentialOptions options, string file)
        {
            _currentFilePath = file;
            _teamcityService = new TeamCityServiceClient(options.TeamCityUrl, options.TeamCityToken);
            _webhook = options.Webhook;
            _buildId = options.BuildId;
            _output = options.Output;
        }

        public async Task RunAsync()
        {
            await Task.Yield();
            var baseFile = RetrieveBaseFile();
            Console.WriteLine($"Retrieving base file: {baseFile}");

            var baseDuplicates = new Dictionary<string, Duplicate>();
            if (File.Exists(baseFile))
            {
                baseDuplicates = Load(baseFile).ToDictionary(x => x.Key);
            }
            var baseKeys = baseDuplicates.Keys.ToHashSet();
            var currentDuplicates = Load(_currentFilePath).ToDictionary(x => x.Key);
            var currentKeys = currentDuplicates.Keys.ToHashSet();
            var newDuplicates = currentDuplicates.Where(x => !baseKeys.Contains(x.Key)).Select(x => x.Value).ToArray();
            var removedDuplicates = baseDuplicates.Where(x => !currentKeys.Contains(x.Key)).Select(x => x.Value).ToArray();

            var nowUtc = DateTime.UtcNow;

            using (var httpClient = new HttpClient())
            {
                foreach (var message in await GetMessages(newDuplicates, removedDuplicates, nowUtc, currentDuplicates.Count))
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
                    $"duplicate-report-{nowUtc.AddDays(-1).ToLocalTime():yyyy_MM_dd}.xml");
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

        private async Task<HttpContent[]> GetMessages(Duplicate[] newDuplicates, Duplicate[] removedDuplicates, DateTime nowUtc, int total)
        {
            var sections = await GetSections(newDuplicates, removedDuplicates, total);

            Console.WriteLine("Creating header section of message");
            var card = new HangoutCard
            {
                Header = CardBuilderHelper.GetCardHeader("<b>Duplicate daily report</b>", nowUtc, "https://icon-icons.com/icons2/1278/PNG/128/1497562286-gemini-zodiac-sign_85087.png"),
                Sections = sections
            };
            var content = JsonConvert.SerializeObject(new HangoutCardMessage
            {
                Cards = new[] {card}
            }, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            });

            Console.WriteLine($"Sending message to Hangout:\r\n{content}");
            return new HttpContent[] { new StringContent(content) };
        }

        private async Task<HangoutCardSection[]> GetSections(Duplicate[] newDuplicates, Duplicate[] removedDuplicates, int total)
        {
            var hasNew = newDuplicates.Length > 0;
            var hasLess = removedDuplicates.Length > 0;

            Console.WriteLine($"Creating section of message for all {total} duplications");
            var sections = new List<HangoutCardSection>
            {
                CardBuilderHelper.GetTextParagraphSection($"The total of duplications in our code base is <b>{total}</b>.")
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

        private static Duplicate[] Load(string filePath)
        {
            var baseElement = XElement.Load(filePath);
            var duplicateNodes = baseElement.Descendants("Duplicate");
            return duplicateNodes.Select(GetDuplicate).ToArray();
        }

        private static Duplicate GetDuplicate(XElement duplicateNode)
        {
            var fragments = duplicateNode.Descendants("Fragment").Select(GetFragment).ToArray();
            return new Duplicate
            {
                Cost = (int)duplicateNode.Attribute("Cost"),
                Fragments = fragments,
                Key = ComputeKey(fragments)
            };
        }

        private static string ComputeKey(Fragment[] fragments)
        {
            var sb = new StringBuilder();
            var isFirst = true;
            foreach (var fragment in fragments)
            {
                if (!isFirst)
                    sb.Append('-');

                sb.Append(fragment.FileName);
                sb.Append('-');
                sb.Append(HashHelper.ComputeHash(fragment.Text));

                isFirst = false;
            }

            return sb.ToString();
        }

        private static Fragment GetFragment(XElement fragmentNode)
        {
            var offsetRangeNode = fragmentNode.Element("OffsetRange");
            var lineRangeNode = fragmentNode.Element("LineRange");
            return new Fragment
            {
                FileName = (string)fragmentNode.Element("FileName"),
                Text = (string)fragmentNode.Element("Text"),
                Lines = new Range { Start = (int)lineRangeNode?.Attribute("Start"), End = (int)lineRangeNode?.Attribute("End") },
                Offset = new Range { Start = (int)offsetRangeNode?.Attribute("Start"), End = (int)offsetRangeNode?.Attribute("End") }
            };
        }
    }
}
