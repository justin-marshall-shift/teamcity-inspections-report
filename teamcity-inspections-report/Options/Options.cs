using System.Collections.Generic;
using CommandLine;

namespace teamcity_inspections_report.Options
{
    [Verb("duplicates", HelpText = "Compute and report the differential of duplication analysis.")]
    public class DifferentialOptions
    {
        [Option('f', "folder", Required = true, HelpText = "Folder where to find the duplicate report")]
        public string Folder { get; set; }
        [Option('w', "webhook", Required = true, HelpText = "Webhook of the room where you want to post messages")]
        public string Webhook { get; set; }

        [Option('b', "build", Required = true, HelpText = "Id of the inspection build")]
        public long BuildId { get; set; }

        [Option('u', "url", Required = true, HelpText = "TeamCity server url.")]
        public string TeamCityUrl { get; set; }

        [Option('t', "token", Required = true, HelpText = "TeamCity REST API token.")]
        public string TeamCityToken { get; set; }

        [Option('o', "output", Required = true, HelpText = "Folder where the reports will be archived")]
        public string Output { get; set; }

        [Option('g', "git", Required = true, HelpText = "Git repository path")]
        public string Git { get; set; }
    }

    [Verb("inspection", HelpText = "Compute and report the differential of duplication analysis.")]
    public class InspectionOptions
    {
        [Option('f', "folder", Required = true, HelpText = "Folder where to find the inspection report")]
        public string Folder { get; set; }

        [Option('h', "threshold", Required = true, HelpText = "Threshold file for the projects")]
        public string Threshold { get; set; }

        [Option('s', "solution", Required = false, HelpText = "Relative path to the solution analyzed")]
        public string Solution { get; set; }

        [Option('l', "login", Required = false, Hidden = true, HelpText = "SMTP account login")]
        public string Login { get; set; }

        [Option('p', "password", Required = false, Hidden = true, HelpText = "SMTP account password")]
        public string Password { get; set; }

        [Option('j', "jiraLogin", Required = false, HelpText = "Jira login")]
        public string JiraLogin { get; set; }

        [Option('a', "jiraPassword", Required = false, HelpText = "Jira password")]
        public string JiraPassword { get; set; }
        [Option('w', "webhook", Required = true, HelpText = "Webhook of the room where you want to post messages")]
        public string Webhook { get; set; }

        [Option('b', "build", Required = true, HelpText = "Id of the inspection build")]
        public long BuildId { get; set; }

        [Option('u', "url", Required = true, HelpText = "TeamCity server url.")]
        public string TeamCityUrl { get; set; }

        [Option('t', "token", Required = true, HelpText = "TeamCity REST API token.")]
        public string TeamCityToken { get; set; }

        [Option('o', "output", Required = true, HelpText = "Folder where the reports will be archived")]
        public string Output { get; set; }

        [Option('g', "git", Required = true, HelpText = "Git repository path")]
        public string Git { get; set; }
    }

    [Verb("mail", HelpText = "Test the sending of a mail")]
    public class MailTestOptions
    {
        [Option('l', "login", Required = true, HelpText = "SMTP account login")]
        public string Login { get; set; }

        [Option('p', "password", Required = true, HelpText = "SMTP account password")]
        public string Password { get; set; }

        [Option('m', "mail", Required = true, HelpText = "Mail to.")]
        public string MailTo { get; set; }
    }

    [Verb("deprecate", HelpText = "Check status on github of deprecated status check")]
    public class DeprecatedOptions
    {
        [Option('u', "url", Required = true, HelpText = "TeamCity server url.")]
        public string TeamCityUrl { get; set; }

        [Option('t', "token", Required = true, HelpText = "TeamCity REST API token.")]
        public string TeamCityToken { get; set; }

        [Option('c', "configurations", Required = true, HelpText = "List of configuration that have been deprecated for status checks", Hidden = true, Separator = ';')]
        public IEnumerable<string> Configurations { get; set; }

        [Option('g', "githubToken", Required = true, HelpText = "Github access token")]
        public string GithubToken { get; set; }

        [Option('b', "buildId", Required = true, HelpText = "Build id")]
        public long BuildId { get; set; }
    }

    [Verb("blame", HelpText = "Blame or congratulate users on results of the inspections")]
    public class BlameOptions
    {
        [Option('g', "git", Required = true, HelpText = "Git repository path")]
        public string Git { get; set; }

        [Option('o', "old", Required = true, HelpText = "Old inspection report path")]
        public string Old { get; set; }

        [Option('n', "new", Required = true, HelpText = "New inspection report path")]
        public string New { get; set; }

        [Option('b', "baseCommit", Required = true, HelpText = "BaseCommit")]
        public string BaseCommit { get; set; }

        [Option('h', "headCommit", Required = true, HelpText = "BaseCommit")]
        public string HeadCommit { get; set; }

        [Option('s', "solution", Required = false, HelpText = "Relative path to the solution analyzed")]
        public string Solution { get; set; }
    }

    [Verb("releaseNotesMetadata", HelpText = "Generate the metadata files for releases notes")]
    public class ReleaseNotesMetadataOptions
    {
        [Option('b', "build", Required = true, HelpText = "Id of the inspection build")]
        public long BuildId { get; set; }

        [Option('u', "url", Required = true, HelpText = "TeamCity server url.")]
        public string TeamCityUrl { get; set; }

        [Option('t', "token", Required = true, HelpText = "TeamCity REST API token.")]
        public string TeamCityToken { get; set; }

        [Option('m', "metadata", Required = true, HelpText = "Path to the metadata file")]
        public string Metadata { get; set; }

        [Option('w', "webhook", Required = true, HelpText = "Webhook of the room where you want to post messages")]
        public string Webhook { get; set; }
    }

    [Verb("derivation", HelpText = "Check from how long a PR derived from develop")]
    public class DerivationOptions
    {
        [Option("scope", Group = "mode", HelpText = "Scope the analysis to the current branch", Required = false)]
        public bool IsScoped { get; set; }

        [Option("integral", Group = "mode", HelpText = "Scan all the active branches", Required = false)]
        public bool IsGlobal { get; set; }

        [Option("dry-run", HelpText = "Scan all the active branches", Required = false)]
        public bool DryRun { get; set; }

        [Option('b', "build", Required = true, HelpText = "Id of the build")]
        public long BuildId { get; set; }

        [Option('u', "teamcityUrl", Required = true, HelpText = "TeamCity server url")]
        public string TeamCityUrl { get; set; }

        [Option('t', "teamcityToken", Required = true, HelpText = "TeamCity REST API token")]
        public string TeamCityToken { get; set; }

        [Option('g', "githubToken", Required = true, HelpText = "Github access token")]
        public string GithubToken { get; set; }

        [Option('r', "repository", Required = true, HelpText = "Git repository path")]
        public string Repository { get; set; }

        [Option('m', "maxDerivation", Required = true, HelpText = "Maximum derivation in days")]
        public int Derivation { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output folder")]
        public string Output { get; set; }
    }
}
