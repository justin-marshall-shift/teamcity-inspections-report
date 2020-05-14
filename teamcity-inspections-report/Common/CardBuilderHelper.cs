using System;
using System.Threading.Tasks;
using teamcity_inspections_report.Hangout;

namespace teamcity_inspections_report.Common
{
    public static class CardBuilderHelper
    {
        public static HangoutCardHeader GetCardHeader(string title, DateTime nowUtc, string imageUrl)
        {
            return new HangoutCardHeader
            {
                Title = title,
                Subtitle = $"{nowUtc.ToLocalTime():D}",
                ImageStyle = "IMAGE",
                ImageUrl = imageUrl
            };
        }

        public static HangoutCardSection GetTextParagraphSection(string text)
        {
            return new HangoutCardSection
            {
                Widgets = new HangoutCardWidget[]
                {
                    new HangoutCardTextParagraphWidget
                    {
                        TextParagraph = new HangoutCardText
                        {
                            Text = text
                        }
                    }
                }
            };
        }

        public static async Task<HangoutCardSection> GetLinkSectionToTeamCityBuild(string token, string url, long buildId, string tab)
        {
            var teamcityBuildUrl = await TeamCityHelper.GetTeamCityBuildUrl(token, url, buildId, tab);

            Console.WriteLine("Adding link to teamcity");
            return new HangoutCardSection
            {
                Widgets = new HangoutCardWidget[]
                {
                    new HangoutCardClickWidget
                    {
                        Buttons = new[]
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
            };
        }
    }
}
