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
                            new HangoutCardTextButton
                            {
                                TextButton = new HangoutCardButton
                                {
                                    Text = "Go to TeamCity build",
                                    OnClick = new HangoutOnClickAction
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

        public static HangoutCardSection GetKeyValueSection(string topLabel, string content, string bottomLabel, string iconUrl)
        {
            return new HangoutCardSection
            {
                Widgets = new HangoutCardWidget[]
                {
                    new HangoutKeyValueWidget
                    {
                        KeyValue = new HangoutKeyValue
                        {
                            TopLabel = topLabel,
                            Content = content,
                            BottomLabel = bottomLabel,
                            ContentMultiline = true,
                            IconUrl = iconUrl
                        }
                    }
                }
            };

        }
    }
}
