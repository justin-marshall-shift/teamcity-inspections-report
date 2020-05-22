using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using teamcity_inspections_report.Common;
using teamcity_inspections_report.Hangout;
using teamcity_inspections_report.Options;

namespace teamcity_inspections_report.Reporters
{
    public class ReleaseNotesMetadataGenerator
    {
        private readonly TeamCityServiceClient _teamCityService;
        private readonly long _buildId;
        private readonly string _path;
        private readonly string _webhook;

        public ReleaseNotesMetadataGenerator(ReleaseNotesMetadataOptions options)
        {
            _teamCityService = new TeamCityServiceClient(options.TeamCityUrl, options.TeamCityToken);
            _buildId = options.BuildId;
            _path = options.Metadata;
            _webhook = options.Webhook;
        }

        public async Task RunAsync()
        {
            Console.WriteLine($"Current user is: {System.Security.Principal.WindowsIdentity.GetCurrent().Name}");
            var (baseCommit, headCommit) = await _teamCityService.ComputeCommitRange(_buildId);

            var metadata = new MetadataReleaseNotes
            {
                BaseCommit = baseCommit,
                HeadCommit = headCommit
            };

            var content = JsonConvert.SerializeObject(metadata);

            if (File.Exists(_path)) File.Delete(_path);

            using (var file = File.OpenWrite(_path))
            using (var writer = new StreamWriter(file))
            {
                await writer.WriteAsync(content);
                await writer.FlushAsync();
            }

            using (var httpClient = new HttpClient())
            {
                var message = await GetMessage(DateTime.UtcNow);
                Console.WriteLine($"Webhook: {_webhook}");
                var response = await httpClient.PostAsync(_webhook, message);

                Console.WriteLine($"Response: {response.StatusCode.ToString()}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                }
            }
        }

        private async Task<HttpContent> GetMessage(DateTime utcNow)
        {
            Console.WriteLine("Creating header section of message");
            var card = await GetReportCard(utcNow);

            var content = JsonConvert.SerializeObject(new HangoutCardMessage
            {
                Cards = new []{ card }
            }, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            });

            Console.WriteLine($"Sending message to Hangout:\r\n{content}");
            return new StringContent(content);
        }

        private async Task<HangoutCard> GetReportCard(DateTime utcNow)
        {
            var url = await _teamCityService.GetTeamCityBuildUrl(_buildId, "&tab=report_project117_Release_Note");
            var section = await CardBuilderHelper.GetLinkSectionToUrl(url, "Release notes");

            var sections = new []{ section };
            return new HangoutCard
            {
                Header = CardBuilderHelper.GetCardHeader("<b>Daily release note</b>", utcNow, "https://icon-icons.com/icons2/560/PNG/128/Dynamic-Content_icon-icons.com_53724.png"),
                Sections = sections
            };
        }
    }
}
