using Newtonsoft.Json;

namespace teamcity_inspections_report.Hangout
{
    public class HangoutSimpleMessage
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class HangoutCardMessage
    {
        [JsonProperty("cards")]
        public HangoutCard[] Cards { get; set; }
    }

    public class HangoutCard
    {
        [JsonProperty("header")]
        public HangoutCardHeader Header { get; set; }

        [JsonProperty("sections")]
        public HangoutCardSection[] Sections { get; set; }
    }

    public class HangoutCardHeader
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("subtitle")]
        public string Subtitle { get; set; }

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty("imageStyle")]
        public string ImageStyle { get; set; }
    }

    public class HangoutCardSection
    {
        [JsonProperty("widgets")]
        public HangoutCardWidget[] Widgets { get; set; }
    }

    public abstract class HangoutCardWidget
    {
    }

    public class HangoutCardClickWidget : HangoutCardWidget
    {
        [JsonProperty("buttons")]
        public HangoutCardButton[] Buttons { get; set; }
    }

    public class HangoutCardImageWidget : HangoutCardWidget
    {
        [JsonProperty("image")]
        public HangoutCardImage Image { get; set; }
    }

    public class HangoutCardTextParagraphWidget : HangoutCardWidget
    {
        [JsonProperty("textParagraph")]
        public HangoutCardText TextParagraph { get; set; }
    }

    public class HangoutCardImage
    {
        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }
    }

    public class HangoutCardButton
    {
        [JsonProperty("textButton")]
        public HangoutCardTextButton TextButton { get; set; }
    }

    public class HangoutCardTextButton
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("onClick")]
        public HangoutCardTextButtonAction OnClick { get; set; }
    }

    public class HangoutCardTextButtonAction
    {
        [JsonProperty("openLink")]
        public HangoutCardLink OpenLink { get; set; }
    }

    public class HangoutCardLink
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class HangoutCardText
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
