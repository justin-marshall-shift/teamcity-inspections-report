using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ToolKit.Common;
using ToolKit.Common.Hangout;
using ToolKit.Common.TeamCity;
using ToolKit.Options;

namespace ToolKit.Reporters
{
    public class ReleaseNotesMetadataGenerator
    {
        private readonly TeamCityServiceClient _teamCityService;
        private readonly long _buildId;
        private readonly string _path;
        private readonly HangoutService _hangout;

        public ReleaseNotesMetadataGenerator(ReleaseNotesMetadataOptions options)
        {
            _teamCityService = new TeamCityServiceClient(options.TeamCityUrl, options.TeamCityToken);
            _buildId = options.BuildId;
            _path = options.Metadata;
            _hangout = new HangoutService(options.Webhook);
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

            await _hangout.SendCards(await GetMessages(DateTime.UtcNow));
        }

        private async Task<HangoutCard[]> GetMessages(DateTime utcNow)
        {
            Console.WriteLine("Creating header section of message");
            var card = await GetReportCard(utcNow);

            return new[] { card };
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
