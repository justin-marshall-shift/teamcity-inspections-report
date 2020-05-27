using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ToolKit.Common.GitHelper
{
    public class GitBlameOutputHandler
    {
        private readonly List<GitBlameOutput> _outputs = new List<GitBlameOutput>();

        private const string Author = "author ";
        private const string AuthorMail = "author-mail ";
        private const string HeaderLinePattern = "([a-f0-9]{40}) ([0-9]{1,}) ([0-9]{1,})(.*)";

        private GitBlameOutput _currentOutput;
        private string _currentAuthorMail;
        private string _currentAuthor;

        public void ReadLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;

            if (line.StartsWith(Author))
            {
                _currentAuthor = _currentOutput.Author = line.Remove(0, Author.Length).Trim();
            }

            if (line.StartsWith(AuthorMail))
            {
                _currentAuthorMail = _currentOutput.AuthorMail = line.Remove(0, AuthorMail.Length).Trim().Trim('<', '>');
            }

            var matches = Regex.Match(line, HeaderLinePattern);

            if (!matches.Success)
                return;

            var commit = matches.Groups[1].Value;
            var lineNumber = int.Parse(matches.Groups[3].Value);
            var oldLineNumber = int.Parse(matches.Groups[2].Value);
            _currentOutput = new GitBlameOutput
            {
                NewLine = lineNumber,
                OldLine = oldLineNumber,
                AuthorMail = _currentAuthorMail,
                Author = _currentAuthor,
                Commit = commit
            };
            _outputs.Add(_currentOutput);
        }

        public GitBlameOutput[] GetOutputs()
        {
            return _outputs.ToArray();
        }
    }

    public class GitBlameOutput
    {
        public string Author { get; set; }
        public string AuthorMail { get; set; }
        public int NewLine { get; set; }
        public int OldLine { get; set; }
        public string Commit { get; set; }
    }
}
