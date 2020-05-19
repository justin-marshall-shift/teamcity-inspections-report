using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using teamcity_inspections_report.Common.GitHelper;
using teamcity_inspections_report.Duplicates;
using teamcity_inspections_report.Inspection;

namespace teamcity_inspections_report.Common
{
    public class GitBlamer
    {
        private readonly Git _git;

        public GitBlamer(string repositoryPath)
        {
            _git = new Git(repositoryPath);
        }

        private async Task<Contributor[]> GetNewContributors(string commit, CodeFragment[] fragments)
        {
            await _git.Checkout(commit);

            var groupsByFile = fragments.GroupBy(f => f.Path).Select(g => new { g.Key, Fragments = g.OrderBy(f => f.Lines.Start).ToArray() }).ToArray();
            var contributors = new Dictionary<string, Contributor>();

            foreach (var group in groupsByFile)
            {
                Console.WriteLine($"Analyzing contributions on file {group.Key}");
                var potentialBlames = (await _git.Blame(group.Key)).ToDictionary(b => b.NewLine);

                foreach (var fragment in group.Fragments)
                {
                    for (var line = fragment.Lines.Start; line <= fragment.Lines.End; line++)
                    {
                        if (!potentialBlames.TryGetValue(line, out var blame)) continue;

                        if (!contributors.TryGetValue(blame.Author, out var contributor))
                        {
                            contributor = new Contributor
                            {
                                Mail = blame.AuthorMail,
                                Name = blame.Author,
                                Contributions = new HashSet<CodeFragment>()
                            };
                            contributors[blame.Author] = contributor;
                        }
                        contributor.Contributions.Add(fragment);
                    }
                }
            }

            return contributors.Values.ToArray();
        }

        private async Task<Contributor[]> GetRemovalContributors(string baseCommit, string headCommit, CodeFragment[] fragments)
        {
            await _git.Checkout("develop");

            var groupsByFile = fragments.GroupBy(f => f.Path).Select(g => new { g.Key, Fragments = g.OrderBy(f => f.Lines.Start).ToArray() }).ToArray();
            var contributors = new Dictionary<string, Contributor>();

            var logOutputs = (await _git.ReverseLog(string.Empty, baseCommit, headCommit));
            var lastCommit = logOutputs.First().Commit;
            var nextCommit = new Dictionary<string, GitLogOutput>();
            foreach (var output in logOutputs.Skip(1))
            {
                nextCommit[lastCommit] = output;
                lastCommit = output.Commit;
            }
            nextCommit[lastCommit] = logOutputs.Last();

            foreach (var group in groupsByFile)
            {
                Console.WriteLine($"Analyzing good contributions on file {group.Key}");
                var potentialBlames = (await _git.BlameReverse(group.Key, baseCommit, headCommit)).ToDictionary(b => b.NewLine);

                foreach (var fragment in group.Fragments)
                {
                    for (var line = fragment.Lines.Start; line <= fragment.Lines.End; line++)
                    {
                        if (!potentialBlames.TryGetValue(line, out var blame))
                            continue;

                        if(!nextCommit.TryGetValue(blame.Commit, out var commit))
                            continue;

                        if (!contributors.TryGetValue(commit.Author, out var contributor))
                        {
                            contributor = new Contributor
                            {
                                Mail = commit.AuthorMail,
                                Name = commit.Author,
                                Contributions = new HashSet<CodeFragment>()
                            };
                            contributors[commit.Author] = contributor;
                        }
                        contributor.Contributions.Add(fragment);
                    }
                }
            }

            return contributors.Values.ToArray();
        }

        public async Task<Contributor[]> GetNewContributors(string headCommit, Issue[] newIssues, int number, Func<string, string> computePathToRepo)
        {
            var collaborators = await GetNewContributors(headCommit, newIssues.Select(i => new CodeFragment
            {
                Path = computePathToRepo(i.File),
                Lines = new Range
                {
                    End = i.Line,
                    Start = i.Line
                },
                Score = i.Severity == Severity.WARNING ? 1 : 10
            }).ToArray());

            return collaborators.OrderByDescending(c => c.Contributions.Sum(co => co.Score)).Take(number).ToArray();
        }

        public async Task<Contributor[]> GetRemovalContributors(string baseCommit, string headCommit, Issue[] removedIssues, int number, Func<string, string> computePathToRepo)
        {
            var collaborators = await GetRemovalContributors(baseCommit, headCommit, removedIssues.Select(i => new CodeFragment
            {
                Path = computePathToRepo(i.File),
                Lines = new Range
                {
                    End = i.Line,
                    Start = i.Line
                },
                Score = i.Severity == Severity.WARNING ? 1 : 10
            }).ToArray());

            return collaborators.OrderByDescending(c => c.Contributions.Sum(co => co.Score)).Take(number).ToArray();
        }

        public async Task<Contributor[]> GetRemovalContributors(string baseCommit, string headCommit, Duplicate[] removedDuplicates, int number, Func<string, string> computePathToRepo)
        {
            var collaborators = await GetRemovalContributors(baseCommit, headCommit, removedDuplicates.SelectMany(d => d.Fragments.Select(f => new CodeFragment
            {
                Path = computePathToRepo(f.FileName),
                Lines = f.Lines,
                Score = d.Cost / d.Fragments.Length
            })).ToArray());

            return collaborators.OrderByDescending(c => c.Contributions.Sum(co => co.Score)).Take(number).ToArray();
        }
    }

    public class CodeFragment
    {
        public string Path { get; set; }
        public Range Lines { get; set; }
        public int Score { get; set; }
    }

    public class Contributor
    {
        public string Name { get; set; }
        public string Mail { get; set; }
        public HashSet<CodeFragment> Contributions { get; set; }
    }
}
