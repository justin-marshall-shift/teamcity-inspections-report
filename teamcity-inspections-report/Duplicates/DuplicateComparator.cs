using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using teamcity_inspections_report.Common;

namespace teamcity_inspections_report.Duplicates
{
    public class DuplicateComparator
    {
        private readonly string _formerInspection;
        private readonly string _currentInspection;

        private readonly IDictionary<string,string> _forbiddenStrings = new Dictionary<string, string>{{"&#x1A;", "oe"}};

        public DuplicateComparator(string formerInspection, string currentInspection)
        {
            _formerInspection = formerInspection;
            _currentInspection = currentInspection;
        }

        public (Duplicate[] newDuplicates, Duplicate[] removedDuplicates, Duplicate[] currentDuplicates) GetComparison()
        {
            var baseDuplicates = new Dictionary<string, Duplicate>();
            if (File.Exists(_formerInspection))
            {
                baseDuplicates = Load(_formerInspection).ToDictionary(x => x.Key);
            }
            var baseKeys = baseDuplicates.Keys.ToHashSet();
            var currentDuplicates = Load(_currentInspection).ToDictionary(x => x.Key);
            var currentKeys = currentDuplicates.Keys.ToHashSet();
            var newDuplicates = currentDuplicates.Where(x => !baseKeys.Contains(x.Key)).Select(x => x.Value).ToArray();
            var removedDuplicates = baseDuplicates.Where(x => !currentKeys.Contains(x.Key)).Select(x => x.Value).ToArray();
            return (newDuplicates, removedDuplicates, currentDuplicates.Values.ToArray());
        }

        private Duplicate[] Load(string filePath)
        {
            // TODO modify this code for a more clean solution
            var text = File.ReadAllText(filePath);
            foreach (var (key, value) in _forbiddenStrings)
            {
                text = text.Replace(key, value);
            }
            using (var textReader = new StringReader(text))
            {
                var baseElement = XElement.Load(textReader);
                var duplicateNodes = baseElement.Descendants("Duplicate");
                return duplicateNodes.Select(GetDuplicate).ToArray();
            }
        }

        private static Duplicate GetDuplicate(XElement duplicateNode)
        {
            var fragments = duplicateNode.Descendants("Fragment").Select(GetFragment).ToArray();
            return new Duplicate
            {
                Cost = (int)duplicateNode.Attribute("Cost"),
                Fragments = fragments,
                Key = ComputeKey(fragments)
            };
        }

        private static string ComputeKey(Fragment[] fragments)
        {
            var sb = new StringBuilder();
            var isFirst = true;
            foreach (var fragment in fragments)
            {
                if (!isFirst)
                    sb.Append('-');

                sb.Append(fragment.FileName);
                sb.Append('-');
                sb.Append(HashHelper.ComputeHash(fragment.Text));

                isFirst = false;
            }

            return sb.ToString();
        }

        private static Fragment GetFragment(XElement fragmentNode)
        {
            var offsetRangeNode = fragmentNode.Element("OffsetRange");
            var lineRangeNode = fragmentNode.Element("LineRange");
            return new Fragment
            {
                FileName = (string)fragmentNode.Element("FileName"),
                Text = (string)fragmentNode.Element("Text"),
                Lines = new Range { Start = (int)lineRangeNode?.Attribute("Start"), End = (int)lineRangeNode?.Attribute("End") },
                Offset = new Range { Start = (int)offsetRangeNode?.Attribute("Start"), End = (int)offsetRangeNode?.Attribute("End") }
            };
        }
    }
}
