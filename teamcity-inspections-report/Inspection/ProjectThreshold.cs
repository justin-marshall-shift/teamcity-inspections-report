using CsvHelper.Configuration.Attributes;

namespace teamcity_inspections_report.Inspection
{
    public class ProjectThreshold
    {
        [Name("Project")]
        public string Project { get; set; }

        [Name("InspectionsThreshold")]
        public int Threshold { get; set; }
    }
}
