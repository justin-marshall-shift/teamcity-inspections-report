using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using teamcity_inspections_report.Duplicates;
using teamcity_inspections_report.Hangout;
using teamcity_inspections_report.TeamCityService;
using File = System.IO.File;

namespace teamcity_inspections_report.Reporters
{
    public class DifferentialReporter
    {
        private readonly string _currentFilePath;
        private readonly string _webhook;
        private readonly long _buildId;
        private readonly string _teamCityUrl;
        private readonly string _teamCityToken;
        private readonly string _output;

        public DifferentialReporter(string currentFilePath, string webhook, long buildId, string teamCityUrl, string teamCityToken, string output)
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

            var nowUtc = DateTime.UtcNow.Date;

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
            var teamcityBuildUrl = await GetTeamCityBuildUrl();

            var sections = GetSections(newDuplicates, removedDuplicates, teamcityBuildUrl, total);

            Console.WriteLine("Creating header section of message");
            var card = new HangoutCard
            {
                Header = new HangoutCardHeader
                {
                    Title = "Duplicate report",
                    Subtitle = $"{nowUtc.ToLocalTime():D}"
                },
                Sections = sections
            };
            var content = JsonConvert.SerializeObject(new HangoutCardMessage
            {
                Cards = new []{ card }
            });

            Console.WriteLine($"Sending message to Hangout:\r\n{content}");
            return new HttpContent[] { new StringContent(content) };
        }

        private async Task<string> GetTeamCityBuildUrl()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _teamCityToken);
            httpClient.BaseAddress = new Uri(_teamCityUrl);
            var client = new Client(httpClient) { BaseUrl = _teamCityUrl };
            var build = await client.ServeBuildAsync($"id:{_buildId}", null);

            var uriBuilder = new UriBuilder(build.WebUrl) { Scheme = Uri.UriSchemeHttp };

            Console.WriteLine($"Retrieving build url: {uriBuilder}");
            return uriBuilder.ToString();
        }

        private HangoutCardSection[] GetSections(Duplicate[] newDuplicates, Duplicate[] removedDuplicates, string teamcityBuildUrl, int total)
        {
            var hasNew = newDuplicates.Length > 0;
            var hasLess = removedDuplicates.Length > 0;

            Console.WriteLine($"Creating section of message for all {total} duplications");
            var sections = new List<HangoutCardSection>
            {
                new HangoutCardSection
                {
                    Widgets = new HangoutCardWidget[]
                    {
                        new HangoutCardTextParagraphWidget
                        {
                            TextParagraph = new HangoutCardText
                            {
                                Text = $"The total of duplications in our code base is <b>{total}</b>."
                            }
                        }
                    }
                }
            };


            if (!hasNew && !hasLess)
            {
                Console.WriteLine("No change in duplications");
                sections.Add(new HangoutCardSection
                {
                    Widgets = new HangoutCardWidget[]
                    {
                        new HangoutCardTextParagraphWidget
                        {
                            TextParagraph = new HangoutCardText
                            {
                                Text = "No change was found during the inspection"
                            }
                        }
                    }
                });
            }

            if (hasNew)
            {
                Console.WriteLine($"Adding section for the {newDuplicates.Length} new duplicates");
                sections.Add(new HangoutCardSection
                {
                    Widgets = new HangoutCardWidget[]
                    {
                        new HangoutCardTextParagraphWidget
                        {
                            TextParagraph = new HangoutCardText
                            {
                                Text = $"<b>{newDuplicates.Length}</b> duplication{(newDuplicates.Length == 1 ? "has" : "s have")} been introduced."
                            }
                        }
                    }
                });
            }

            if (hasLess)
            {
                Console.WriteLine($"Adding section for the {removedDuplicates.Length} removed duplicates");
                sections.Add(new HangoutCardSection
                {
                    Widgets = new HangoutCardWidget[]
                    {
                        new HangoutCardTextParagraphWidget
                        {
                            TextParagraph = new HangoutCardText
                            {
                                Text = $"<b>{removedDuplicates.Length}</b> duplication{(removedDuplicates.Length == 1 ? "has" : "s have")} been removed."
                            }
                        }
                    }
                });
            }

            Console.WriteLine("Adding link to teamcity");
            sections.Add(new HangoutCardSection
            {
                Widgets = new HangoutCardWidget[]
                {
                    new HangoutCardClickWidget
                    {
                        Buttons = new []
                        {
                            new HangoutCardButton
                            {
                                TextButton = new HangoutCardTextButton
                                {
                                    Text = "Go to TeamCity build",
                                    OnClick = new HangoutCardTextButtonAction
                                    {
                                        OpenLink = new HangoutCardLink
                                        {
                                            Url = teamcityBuildUrl
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });

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
                sb.Append(ComputeHash(fragment.Text));

                isFirst = false;
            }

            return sb.ToString();
        }

        private static string ComputeHash(string fragment)
        {
            var text = Clean(fragment);

            if (string.IsNullOrEmpty(text))
                return string.Empty;

            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                var textData = Encoding.UTF8.GetBytes(text);
                var hash = sha.ComputeHash(textData);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        private static string Clean(string fragment)
        {
            var elements = fragment.Split(new[] { '\t', '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', elements);
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
