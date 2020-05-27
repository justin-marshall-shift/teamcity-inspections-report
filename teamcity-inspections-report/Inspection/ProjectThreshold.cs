using CsvHelper.Configuration.Attributes;

namespace ToolKit.Inspection
{
    public class ProjectThreshold
    {
        [Name("Project")]
        public string Project { get; set; }

        [Name("InspectionsThreshold")]
        public int Threshold { get; set; }
    }
}
