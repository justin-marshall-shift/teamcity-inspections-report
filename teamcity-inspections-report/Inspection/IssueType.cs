// ReSharper disable InconsistentNaming
namespace ToolKit.Inspection
{
    public enum Severity
    {
        WARNING,
        ERROR
    }

    public class IssueType
    {
        public string Id { get; set; }
        public string Category { get; set; }
        public string CategoryId { get; set; }
        public string Description { get; set; }
        public Severity Severity { get; set; }
        public string Wiki { get; set; }
    }
}
