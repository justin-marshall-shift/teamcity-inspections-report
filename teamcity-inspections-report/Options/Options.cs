using System.Collections.Generic;
using CommandLine;

namespace teamcity_inspections_report.Options
{
    public abstract class CommonOptions
    {
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
    }

    [Verb("duplicates", HelpText = "Compute and report the differential of duplication analysis.")]
    public class DifferentialOptions : CommonOptions
    {
        [Option('f', "folder", Required = true, HelpText = "Folder where to find the duplicate report")]
        public string Folder { get; set; }
    }

    [Verb("inspection", HelpText = "Compute and report the differential of duplication analysis.")]
    public class InspectionOptions : CommonOptions
    {
        [Option('f', "folder", Required = true, HelpText = "Folder where to find the inspection report")]
        public string Folder { get; set; }

        [Option('h', "threshold", Required = true, HelpText = "Threshold file for the projects")]
        public string Threshold { get; set; }
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

    [Verb("releaseNotes", HelpText = "[WIP] Generate the release notes of develop")]
    public class ReleaseNotesOptions
    {
        [Option('b', "build", Required = true, HelpText = "Id of the inspection build")]
        public long BuildId { get; set; }

        [Option('u', "url", Required = true, HelpText = "TeamCity server url.")]
        public string TeamCityUrl { get; set; }

        [Option('t', "token", Required = true, HelpText = "TeamCity REST API token.")]
        public string TeamCityToken { get; set; }

        [Option('o', "output", Required = true, HelpText = "Folder where the reports will be archived")]
        public string Output { get; set; }

        [Option('a', "audit", Required = true, HelpText = "Path of the audit file")]
        public string Audit { get; set; }
        
        [Option('l', "loginJira", Required = true, HelpText = "Jira login")]
        public string Login { get; set; }
        
        [Option('p', "PasswordJira", Required = true, HelpText = "Jira password")]
        public string Password { get; set; }

        [Option('g', "githubToken", Required = true, HelpText = "Github access token")]
        public string GithubToken { get; set; }
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
    }
}
