using Newtonsoft.Json;

namespace ToolKit.Common.Hangout
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
        public HangoutCardTextButton[] Buttons { get; set; }
    }

    public class HangoutKeyValueWidget : HangoutCardWidget
    {
        [JsonProperty("keyValue")]
        public HangoutKeyValue KeyValue { get; set; }
    }

    public class HangoutKeyValue
    {
        [JsonProperty("topLabel")]
        public string TopLabel { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("contentMultiline")]
        public bool ContentMultiline { get; set; }
        [JsonProperty("bottomLabel")]
        public string BottomLabel { get; set; }
        [JsonProperty("iconUrl")]
        public string IconUrl { get; set; }
        [JsonProperty("onClick")]
        public HangoutOnClickAction OnClick { get; set; }
        [JsonProperty("button")]
        public HangoutCardTextButton Button { get; set; }
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

    public class HangoutCardTextButton
    {
        [JsonProperty("textButton")]
        public HangoutCardButton TextButton { get; set; }
    }

    public class HangoutCardButton
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("onClick")]
        public HangoutOnClickAction OnClick { get; set; }
    }

    public class HangoutOnClickAction
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
