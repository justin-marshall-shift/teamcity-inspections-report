﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using teamcity_inspections_report.Options;
using teamcity_inspections_report.Reporters;

namespace teamcity_inspections_report
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<DifferentialOptions, InspectionOptions>(args)
                .WithParsed<DifferentialOptions>(Run)
                .WithParsed<InspectionOptions>(Run)
                .WithNotParsed(Report);
        }

        private static void Run(InspectionOptions options)
        {
            var files = Directory.EnumerateFiles(options.Folder, "inspections.xml");
            var file = files.FirstOrDefault();

            if (file == null)
                throw new ArgumentNullException(nameof(file), "No report file in directory");

            Console.WriteLine($"[Analysis] File: {file}");

            var reporter = new InspectionReporter(file, options.Webhook, options.BuildId, options.TeamCityUrl, options.TeamCityToken, options.Output, options.Threshold);
            reporter.RunAsync().Wait();
        }

        private static void Report(IEnumerable<Error> errs)
        {
            foreach (var error in errs.Select(err => err.Tag).Distinct())
            {
                Console.WriteLine($"{error}");
            }
        }

        private static void Run(DifferentialOptions options)
        {
            var files = Directory.EnumerateFiles(options.Current, "dupfinder-report-*.xml");
            var file = files.FirstOrDefault();

            if (file == null)
                throw new ArgumentNullException(nameof(file), "No report file in directory");

            Console.WriteLine($"[Analysis] File: {file}");

            var reporter = new DifferentialReporter(file, options.Webhook, options.BuildId, options.TeamCityUrl, options.TeamCityToken, options.Output);
            reporter.RunAsync().Wait();
        }
    }
}
