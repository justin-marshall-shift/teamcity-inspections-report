using CsvHelper.Configuration.Attributes;

namespace Teamcity_inspections_report.Common.GitHub
{
    public class BranchInformation
    {
        [Name("Id")]
        public int Id { get; set; }
        [Name("Title")]
        public string Title { get; set; }
        [Name("WIP")]
        public bool IsWip { get; set; }
        [Name("Creation time")]
        public string Creation { get; set; }
        [Name("Last develop merge time")]
        public string DevMerge { get; set; }
        [Name("Derivation (days)")]
        public int Derivation { get; set; }
        [Name("Last develop commit")]
        public string Commit { get; set; }
    }
}
