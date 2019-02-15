// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using Microsoft.ML.Auto;

namespace Microsoft.ML.CLI.Commands
{
    internal static class CommandDefinitions
    {
        internal static System.CommandLine.Command New(ICommandHandler handler)
        {
            var newCommand = new System.CommandLine.Command("new", "ML.NET CLI tool for code generation", handler: handler)
            {
                                //Dataset(), 
                                TrainDataset(),
                                ValidationDataset(),
                                TestDataset(),
                                MlTask(),
                                LabelName(),
                                MaxExplorationTime(),
                                LabelColumnIndex(),
                                Verbosity(),
                                Name(),
                                OutputBaseDir()
            };

            newCommand.Argument.AddValidator((sym) =>
            {
                if (sym.Children["--train-dataset"] == null)
                {
                    return "Option required : --train-dataset";
                }
                if (sym.Children["--ml-task"] == null)
                {
                    return "Option required : --ml-task";
                }
                if (sym.Children["--label-column-name"] == null && sym.Children["--label-column-index"] == null)
                {
                    return "Option required : --label-column-name or --label-column-index";
                }
                return null;
            });

            return newCommand;

            /*Option Dataset() =>
               new Option("--dataset", "Dataset file path.",
                          new Argument<FileInfo>().ExistingOnly()); */

            Option TrainDataset() =>
               new Option("--train-dataset", "Train dataset file path.",
                          new Argument<FileInfo>().ExistingOnly());

            Option ValidationDataset() =>
               new Option("--validation-dataset", "Validation dataset file path.",
                          new Argument<FileInfo>(defaultValue: default(FileInfo)).ExistingOnly());

            Option TestDataset() =>
               new Option("--test-dataset", "Test dataset file path.",
                          new Argument<FileInfo>(defaultValue: default(FileInfo)).ExistingOnly());

            Option MlTask() =>
               new Option("--ml-task", "Type of ML task.",
                          new Argument<TaskKind>().WithSuggestions(GetMlTaskSuggestions()));

            Option LabelName() =>
               new Option("--label-column-name", "Name of the label column.",
                          new Argument<string>());

            Option LabelColumnIndex() =>
             new Option("--label-column-index", "Index of the label column.",
                        new Argument<uint>());

            Option MaxExplorationTime() =>
              new Option("--max-exploration-time", "Timeout in seconds for exploring models.",
                         new Argument<uint>(defaultValue: 10));

            Option Verbosity() =>
              new Option(new List<string>() { "--verbosity" }, "Verbosity of the output to be shown by the tool.",
                         new Argument<string>(defaultValue: "m").WithSuggestions(GetVerbositySuggestions()));

            Option Name() =>
              new Option(new List<string>() { "--name" }, "Name of the output files(project and folder).",
                         new Argument<string>(defaultValue: "Sample"));

            Option OutputBaseDir() =>
              new Option(new List<string>() { "--output" }, "Output folder path.",
             new Argument<string>(defaultValue: ".\\Sample"));

        }

        private static string[] GetMlTaskSuggestions()
        {
            return Enum.GetValues(typeof(TaskKind)).Cast<TaskKind>().Select(v => v.ToString()).ToArray();
        }

        private static string[] GetVerbositySuggestions()
        {
            return new[] { "q", "m", "diag" };
        }
    }
}
