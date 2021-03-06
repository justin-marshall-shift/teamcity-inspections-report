﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using teamcity_inspections_report.Common;
using teamcity_inspections_report.Options;
using teamcity_inspections_report.Reporters;
using teamcity_inspections_report.Validators;

namespace teamcity_inspections_report
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<DifferentialOptions, InspectionOptions, DeprecatedOptions, BlameOptions, ReleaseNotesMetadataOptions, MailTestOptions, DerivationOptions>(args)
                .WithParsed<DifferentialOptions>(Run)
                .WithParsed<InspectionOptions>(Run)
                .WithParsed<DeprecatedOptions>(Run)
                .WithParsed<BlameOptions>(Run)
                .WithParsed<ReleaseNotesMetadataOptions>(Run)
                .WithParsed<MailTestOptions>(Run)
                .WithParsed<DerivationOptions>(Run)
                .WithNotParsed(Report);
        }

        private static void Run(DerivationOptions options)
        {
            var checker = new DerivationChecker(options);
            checker.RunAsync().Wait();
        }

        private static void Run(MailTestOptions options)
        {
            var mailTester = new MailTester(options);
            mailTester.SendMail().Wait();
        }

        private static void Run(ReleaseNotesMetadataOptions options)
        {
            var generator = new ReleaseNotesMetadataGenerator(options);
            generator.RunAsync().Wait();
        }

        private static void Run(BlameOptions options)
        {
            var reporter = new GitReporter(options);
            reporter.RunAsync().Wait();
        }

        private static void Run(DeprecatedOptions options)
        {
            var validator = new GithubStatusValidator(options);

            validator.RunAsync().Wait();
        }

        private static void Run(InspectionOptions options)
        {
            var files = Directory.EnumerateFiles(options.Folder, "inspections.xml");
            var file = files.FirstOrDefault();

            if (file == null)
                throw new ArgumentNullException(nameof(file), $"No report file in directory {options.Folder}");

            Console.WriteLine($"[Analysis] File: {file}");

            var reporter = new InspectionReporter(options, file);
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
            var files = Directory.EnumerateFiles(options.Folder, "dupfinder-report-*.xml");
            var file = files.FirstOrDefault();

            if (file == null)
                throw new ArgumentNullException(nameof(file), $"No report file in directory {options.Folder}");

            Console.WriteLine($"[Analysis] File: {file}");

            var reporter = new DifferentialReporter(options, file);
            reporter.RunAsync().Wait();
        }
    }
}
