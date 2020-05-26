using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace teamcity_inspections_report.Common.GitHelper
{
    public class GitLogOutputHandler
    {
        private readonly List<GitLogOutput> _outputs = new List<GitLogOutput>();

        private const string AuthorPattern = "Author: (.*) <(.*)>";
        private const string Commit = "commit ";
        private const string Date = "Date: ";


        private GitLogOutput _currentOutput;
        private string _currentAuthor;
        private string _currentAuthorMail;
        private DateTime _currentDateTime;

        public void ReadLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;

            var matches = Regex.Match(line, AuthorPattern);

            if (matches.Success)
            {
                _currentAuthor = _currentOutput.Author = matches.Groups[1].Value;
                _currentAuthorMail = _currentOutput.AuthorMail = matches.Groups[2].Value;
            }

            if (line.StartsWith(Date))
            {
                if (DateTime.TryParse(line.Remove(0, Date.Length).Trim(), out var date))
                    _currentDateTime = _currentOutput.Date = date;
            }

            if (line.StartsWith(Commit))
            {
                var commit = line.Remove(0, Commit.Length).Trim();

                _currentOutput = new GitLogOutput
                {
                    AuthorMail = _currentAuthorMail,
                    Author = _currentAuthor,
                    Commit = commit,
                    Date = _currentDateTime
                };
                _outputs.Add(_currentOutput);
            }
        }

        public GitLogOutput[] GetOutputs()
        {
            return _outputs.ToArray();
        }
    }

    public class GitLogOutput
    {
        public string Author { get; set; }
        public string AuthorMail { get; set; }
        public string Commit { get; set; }
        public DateTime Date { get; set; }
    }
}
