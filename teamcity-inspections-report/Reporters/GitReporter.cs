using System.Threading.Tasks;
using teamcity_inspections_report.Options;

namespace teamcity_inspections_report.Reporters
{
    public class GitReporter
    {
        private readonly string _git;
        private readonly string _new;
        private readonly string _old;
        private readonly string _threshold;

        public GitReporter(BlameOptions options)
        {
            _git = options.Git;
            _new = options.New;
            _old = options.Old;
            _threshold = options.Threshold;
        }

        public Task RunAsync()
        {
            return Task.CompletedTask;
        }
    }
}
