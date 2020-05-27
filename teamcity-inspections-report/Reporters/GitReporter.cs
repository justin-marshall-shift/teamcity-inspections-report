using System;
using System.IO;
using System.Threading.Tasks;
using ToolKit.Common.Git;
using ToolKit.Inspection;
using ToolKit.Options;

namespace ToolKit.Reporters
{
    public class GitReporter
    {
        private readonly string _gitPath;
        private readonly string _relativeSolutionPath;
        private readonly string _new;
        private readonly string _old;
        private readonly string _baseCommit;
        private readonly string _headCommit;

        public GitReporter(BlameOptions options)
        {
            _gitPath = options.Git;
            _new = options.New;
            _old = options.Old;
            _baseCommit = options.BaseCommit;
            _headCommit = options.HeadCommit;
            _relativeSolutionPath = options.Solution;
        }

        public async Task RunAsync()
        {
            var comparer = new InspectionsComparator(_old, _new, null);

            var (newIssues, removedIssues, _) = comparer.GetComparison();

            var blamer = new GitBlamer(_gitPath);

            var contributorsForNewViolations = await blamer.GetNewContributors(_headCommit, newIssues, 10, ComputePathToRepo);
            var contributorsForRemovalViolations = await blamer.GetRemovalContributors(_baseCommit, _headCommit, removedIssues, 10, ComputePathToRepo);

            Console.WriteLine("The top ten collaborators who added violations are:");
            var rank = 1;
            foreach (var collaborator in contributorsForNewViolations)
            {
                Console.WriteLine($"{rank}# {collaborator.Name} with {collaborator.Contributions.Count} fragments");
                rank++;
            }
            Console.WriteLine("");
            Console.WriteLine("The top ten collaborators who removed violations are:");
            var goodRank = 1;
            foreach (var collaborator in contributorsForRemovalViolations)
            {
                Console.WriteLine($"{goodRank}# {collaborator.Name} with {collaborator.Contributions.Count} fragments");
                goodRank++;
            }
        }

        private string ComputePathToRepo(string relativePath)
        {
            if (string.IsNullOrEmpty(_relativeSolutionPath))
                return relativePath;

            var solutionFolder = Path.GetDirectoryName(_relativeSolutionPath);

            var path = Path.GetFullPath(Path.Combine(_gitPath, solutionFolder, relativePath));

            return path.Replace(_gitPath, string.Empty).Trim('/','\\');
        }
    }
}
