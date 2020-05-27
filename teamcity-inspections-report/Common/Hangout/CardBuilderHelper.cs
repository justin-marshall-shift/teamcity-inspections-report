using System;
using System.Threading.Tasks;

namespace ToolKit.Common.Hangout
{
    public static class CardBuilderHelper
    {
        public static HangoutCardHeader GetCardHeader(string title, DateTime nowUtc, string imageUrl)
        {
            return GetCardHeader(title, $"{nowUtc.ToLocalTime():D}", imageUrl);
        }

        public static HangoutCardHeader GetCardHeader(string title, string subtitle, string imageUrl)
        {
            return new HangoutCardHeader
            {
                Title = title,
                Subtitle = subtitle,
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

        public static async Task<HangoutCardSection> GetLinkSectionToUrl(string url, string message)
        {
            await Task.Yield();
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
                                    Text = message,
                                    OnClick = new HangoutOnClickAction
                                    {
                                        OpenLink = new HangoutCardLink
                                        {
                                            Url = url
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
