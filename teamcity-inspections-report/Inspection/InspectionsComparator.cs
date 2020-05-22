using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using CsvHelper;
using teamcity_inspections_report.Common;

namespace teamcity_inspections_report.Inspection
{
    public class InspectionsComparator
    {
        private readonly string _formerInspection;
        private readonly string _currentInspection;
        private readonly string _threshold;
        private Issue[] _cacheNewIssues;
        private Issue[] _cacheRemovedIssues;
        private Issue[] _cacheCurrentIssues;
        private bool _cache;

        public InspectionsComparator(string formerInspection, string currentInspection, string threshold)
        {
            _formerInspection = formerInspection;
            _currentInspection = currentInspection;
            _threshold = threshold;
        }

        public (Issue[] newIssues, Issue[] removedIssues, Issue[] currentIssues) GetComparison()
        {
            if (!_cache)
            {
                var baseIssues = new Dictionary<string, Issue>();
                if (File.Exists(_formerInspection))
                {
                    baseIssues = Load(_formerInspection);
                }

                var baseKeys = baseIssues.Keys.ToHashSet();
                var currentIssues = Load(_currentInspection);
                var currentKeys = currentIssues.Keys.ToHashSet();

                var newIssues = currentIssues.Where(x => !baseKeys.Contains(x.Key)).Select(x => x.Value).ToArray();
                var removedIssues = baseIssues.Where(x => !currentKeys.Contains(x.Key)).Select(x => x.Value).ToArray();

                _cacheCurrentIssues = currentIssues.Values.ToArray();
                _cacheNewIssues = newIssues;
                _cacheRemovedIssues = removedIssues;
                _cache = true;
            }

            return (_cacheNewIssues, _cacheRemovedIssues, _cacheCurrentIssues);
        }

        private Dictionary<string, Issue> Load(string filePath)
        {
            var baseElement = XElement.Load(filePath);
            var issueTypeNodes = baseElement.Descendants("IssueType");
            var projectNodes = baseElement.Descendants("Project");
            var issueTypes = issueTypeNodes.Select(GetIssueType).ToDictionary(i => i.Id);
            return projectNodes.SelectMany(i => GetIssues(i, issueTypes)).ToDictionary(i => i.Key);
        }

        private static IssueType GetIssueType(XElement issueType)
        {
            return new IssueType
            {
                Id = (string)issueType.Attribute("Id"),
                Category = (string)issueType.Attribute("Category"),
                CategoryId = (string)issueType.Attribute("CategoryId"),
                Description = (string)issueType.Attribute("Description"),
                Severity = Enum.Parse<Severity>((string)issueType.Attribute("Severity")),
                Wiki = (string)issueType.Attribute("WikiUrl")
            };
        }

        private Issue[] GetIssues(XElement project, Dictionary<string, IssueType> issueTypes)
        {
            var projectName = (string)project.Attribute("Name");
            var issueNodes = project.Descendants("Issue").Select(x => GetIssue(projectName, x, issueTypes)).ToArray();
            SetKey(issueNodes);
            return issueNodes;
        }

        private static void SetKey(IEnumerable<Issue> issues)
        {
            var groups = issues.GroupBy(i => i.File);

            foreach (var group in groups)
            {
                var computedKeys = new HashSet<string>();
                foreach (var issue in group)
                {
                    issue.Key = ComputeKey(issue, computedKeys);
                }
            }
        }

        private static string ComputeKey(Issue issue, ISet<string> computedKeys)
        {
            var sb = new StringBuilder();
            sb.Append(issue.File);
            sb.Append('-');
            sb.Append(HashHelper.ComputeHash(issue.Message));

            var key = sb.ToString();

            while (computedKeys.Contains(key))
            {
                key += "#";
            }

            computedKeys.Add(key);
            return key;
        }

        private Issue GetIssue(string projectName, XElement issueNode, IReadOnlyDictionary<string, IssueType> issueTypes)
        {
            return new Issue
            {
                File = (string)issueNode.Attribute("File"),
                Line = issueNode.Attribute("Line") == null ? 0 : (int)issueNode.Attribute("Line"),
                Message = (string)issueNode.Attribute("Message"),
                Offset = GetRange((string)issueNode.Attribute("Offset")),
                Project = projectName,
                TypeId = (string)issueNode.Attribute("TypeId"),
                Severity = issueTypes[(string)issueNode.Attribute("TypeId")].Severity
            };
        }

        private static Range GetRange(string offset)
        {
            var parts = offset.Split('-', StringSplitOptions.RemoveEmptyEntries);

            return new Range
            {
                End = int.Parse(parts[1]),
                Start = int.Parse(parts[0])
            };
        }

        public (int numberOfErrors, List<ProjectState> failingProjects) EnforceNumberOfErrorsAndNumberOfViolationsByProject(Issue[] currentIssues)
        {
            var numberOfErrors = currentIssues.Count(i => i.Severity == Severity.ERROR);

            if (!string.IsNullOrEmpty(_threshold) && File.Exists(_threshold)) return (numberOfErrors, new List<ProjectState>());

            var failingProjects = new List<ProjectState>();
            var projectStates = currentIssues.GroupBy(i => i.Project)
                .Select(g => new ProjectState { Project = g.Key, Count = g.Count() }).ToDictionary(x => x.Project);

            using (var reader = new StreamReader(_threshold))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                Console.WriteLine($"Checking the following inspections thresholds by project (see {Path.GetFileName(_threshold)}):");
                foreach (var record in csv.GetRecords<ProjectThreshold>())
                {
                    Console.WriteLine($"{record.Project}: {record.Threshold}");

                    if (!projectStates.TryGetValue(record.Project, out var project) || project.Count <= record.Threshold)
                        continue;

                    failingProjects.Add(project);
                }
            }

            Console.WriteLine("Found the following inspections count per project:");
            foreach (var projectState in projectStates.Values)
            {
                Console.WriteLine($"{projectState.Project}: {projectState.Count}");
            }

            if (failingProjects.Any())
            {
                Console.WriteLine($"{(failingProjects.Count == 1 ? "This project was" : "These tests were")} found above {(failingProjects.Count == 1 ? "its" : "their")} threshold:\r\n{string.Join("\r\n", failingProjects.Select(f => f.Project))}");
            }

            return (numberOfErrors, failingProjects);
        }

    }
}
