using ToolKit.Common;

namespace ToolKit.Inspection
{
    public class Issue
    {
        public string Key { get; set; }
        public string TypeId { get; set; }
        public Range Offset { get; set; }
        public int Line { get; set; }
        public string Message { get; set; }
        public string File { get; set; }
        public string Project { get; set; }
        public Severity Severity { get; set; }
    }
}
