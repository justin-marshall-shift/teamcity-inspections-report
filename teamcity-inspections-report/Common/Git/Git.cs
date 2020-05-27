using System;
using System.Linq;
using System.Threading.Tasks;
using ToolKit.Common.GitHelper;

namespace ToolKit.Common.Git
{
    public class Git
    {
        public Git(string repositoryPath)
        {
            RepositoryPath = repositoryPath;
        }

        public string RepositoryPath { get; }

        public async Task<string> GetCommonAncestorWithDevelop(string commit)
        {
            var result = string.Empty;
            var development = "develop";
            var gitFetch = new ProcessConfig("git", $"merge-base {commit} origin/{development}", (s, b) =>
            {
                if (!b && !string.IsNullOrEmpty(s)) result = s;
                if (b && !string.IsNullOrEmpty(s)) Console.WriteLine($"Error git: {s}");
            }, RepositoryPath);
            await ProcessUtils.RunProcess(gitFetch);
            await Task.Delay(150);
            return result;
        }

        public async Task Checkout(string commit)
        {
            var gitFetch = new ProcessConfig("git", $"checkout {commit}", (s, b) =>
            {
                if (b && !string.IsNullOrEmpty(s)) Console.WriteLine($"Error git: {s}");
            }, RepositoryPath);
            await ProcessUtils.RunProcess(gitFetch);
        }

        public async Task FetchPrune()
        {
            var gitBlame = new ProcessConfig("git", $"fetch --prune", (s, b) =>
            {
                if (!string.IsNullOrEmpty(s)) Console.WriteLine($"{s}");
            }, RepositoryPath);
            await ProcessUtils.RunProcess(gitBlame);
        }

        public async Task<GitBlameOutput[]> Blame(string filePath)
        {
            var handler = new GitBlameOutputHandler();
            var gitBlame = new ProcessConfig("git", $"blame {filePath} -p", (s, b) =>
            {
                if (!b) handler.ReadLine(s);
                if (b && !string.IsNullOrEmpty(s)) Console.WriteLine($"Error git: {s}");
            }, RepositoryPath);
            await ProcessUtils.RunProcess(gitBlame);
            return handler.GetOutputs();
        }

        public async Task<GitBlameOutput[]> BlameReverse(string filePath, string baseCommit, string headCommit)
        {
            var handler = new GitBlameOutputHandler();
            var gitBlame = new ProcessConfig("git", $"blame --reverse {baseCommit}..{headCommit} {filePath} -p",
                (s, b) =>
                {
                    if (!b) handler.ReadLine(s);
                    if (b && !string.IsNullOrEmpty(s)) Console.WriteLine($"Error git: {s}");
                }, RepositoryPath);
            await ProcessUtils.RunProcess(gitBlame);
            return handler.GetOutputs();
        }

        public async Task<GitLogOutput[]> ReverseLog(string filePath, string baseCommit, string headCommit)
        {
            var handler = new GitLogOutputHandler();
            var gitBlame = new ProcessConfig("git",
                $"log --reverse --ancestry-path {baseCommit}..{headCommit} {filePath}", (s, b) =>
                {
                    if (!b) handler.ReadLine(s);
                    if (b && !string.IsNullOrEmpty(s)) Console.WriteLine($"Error git: {s}");
                }, RepositoryPath);
            await ProcessUtils.RunProcess(gitBlame);
            return handler.GetOutputs();
        }

        public async Task<GitLogOutput[]> Log(string filePath, string baseCommit, string headCommit, Range lines)
        {
            var handler = new GitLogOutputHandler();
            var gitBlame = new ProcessConfig("git",
                $"log --ancestry-path {baseCommit}..{headCommit} -L {lines.Start},{lines.End}:{filePath}", (s, b) =>
                {
                    if (!b) handler.ReadLine(s);
                    if (b && !string.IsNullOrEmpty(s)) Console.WriteLine($"Error git: {s}");
                }, RepositoryPath);
            await ProcessUtils.RunProcess(gitBlame);
            return handler.GetOutputs();
        }

        public async Task<GitLogOutput> Log(string commit)
        {
            var handler = new GitLogOutputHandler();
            var gitBlame = new ProcessConfig("git",
                $"log --date=rfc -1 {commit}", (s, b) =>
                {
                    if (!b) handler.ReadLine(s);
                    if (b && !string.IsNullOrEmpty(s)) Console.WriteLine($"Error git: {s}");
                }, RepositoryPath);
            await ProcessUtils.RunProcess(gitBlame);
            await Task.Delay(250);
            return handler.GetOutputs().FirstOrDefault();
        }

        public async Task<DateTime?> GetCommitDate(string commit)
        {
            var result = string.Empty;
            var gitBlame = new ProcessConfig("git",
                $"show -s --format=%cD {commit}", (s, b) =>
                {
                    if (!b && !string.IsNullOrEmpty(s)) result = s;
                    if (b && !string.IsNullOrEmpty(s)) Console.WriteLine($"Error git: {s}");
                }, RepositoryPath);
            await ProcessUtils.RunProcess(gitBlame);
            await Task.Delay(150);
            if (DateTime.TryParse(result, out var date))
                return date;

            return null;
        }
    }
}
