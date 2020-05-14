﻿using System.Collections.Generic;
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
        public string Current { get; set; }
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
}
