using System.Linq;
using System.Threading.Tasks;
using teamcity_inspections_report.Common.GitHelper;

namespace teamcity_inspections_report.Common.Git
{
    public class Git
    {
        public Git(string repositoryPath)
        {
            RepositoryPath = repositoryPath;
        }

        public string RepositoryPath { get; }

        public async Task<string> GetCommonAncestorWithDevelop(string branch)
        {
            var result = string.Empty;
            var development = "develop";
            var gitFetch = new ProcessConfig("git", $"merge-base origin/{branch} origin/{development}", (s, b) =>
            {
                if (!b && s != null) result = s;
            }, RepositoryPath);
            await ProcessUtils.RunProcess(gitFetch);
            await Task.Delay(100);
            return result;
        }

        public async Task Checkout(string commit)
        {
            var gitFetch = new ProcessConfig("git", $"checkout {commit}", (s, b) => { }, RepositoryPath);
            await ProcessUtils.RunProcess(gitFetch);
        }

        public async Task FetchPrune()
        {
            var gitBlame = new ProcessConfig("git", $"fetch --prune", (s, b) => { }, RepositoryPath);
            await ProcessUtils.RunProcess(gitBlame);
        }

        public async Task<GitBlameOutput[]> Blame(string filePath)
        {
            var handler = new GitBlameOutputHandler();
            var gitBlame = new ProcessConfig("git", $"blame {filePath} -p", (s, b) =>
            {
                if (!b) handler.ReadLine(s);
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
                }, RepositoryPath);
            await ProcessUtils.RunProcess(gitBlame);
            await Task.Delay(100);
            return handler.GetOutputs().FirstOrDefault();
        }
    }
}
