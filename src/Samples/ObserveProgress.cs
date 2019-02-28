﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Data.DataView;
using Microsoft.ML;
using Microsoft.ML.Auto;
using Microsoft.ML.Data;

namespace Samples
{
    static class ObserveProgress
    {
        private static string BaseDatasetsLocation = @"../../../../src/Samples/Data";
        private static string TrainDataPath = $"{BaseDatasetsLocation}/taxi-fare-train.csv";
        private static string TestDataPath = $"{BaseDatasetsLocation}/taxi-fare-test.csv";
        private static string ModelPath = $"{BaseDatasetsLocation}/TaxiFareModel.zip";
        private static string LabelColumn = "fare_amount";

        public static void Run()
        {
            MLContext mlContext = new MLContext();

            // STEP 1: Infer columns
            ColumnInferenceResults columnInference = mlContext.Auto().InferColumns(TrainDataPath, LabelColumn, ',');

            // STEP 2: Load data
            TextLoader textLoader = mlContext.Data.CreateTextLoader(columnInference.TextLoaderArgs);
            IDataView trainDataView = textLoader.Read(TrainDataPath);
            IDataView testDataView = textLoader.Read(TestDataPath);

            // STEP 3: Auto inference with a callback configured
            RegressionExperiment autoExperiment = mlContext.Auto().CreateRegressionExperiment(new RegressionExperimentSettings()
            {
                MaxExperimentTimeInSeconds = 60,
                ProgressHandler = new ProgressHandler()
            });
            autoExperiment.Execute(trainDataView, LabelColumn);

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }

    class ProgressHandler : IProgress<RunResult<RegressionMetrics>>
    {
        int iterationIndex;
        public ProgressHandler()
        {
            ConsolePrinter.PrintRegressionMetricsHeader();
        }

        public void Report(RunResult<RegressionMetrics> iterationResult)
        {
            iterationIndex++;
            ConsolePrinter.PrintRegressionMetrics(iterationIndex, iterationResult.TrainerName, iterationResult.ValidationMetrics);
        }
    }

    class ConsolePrinter
    {
        public static void PrintRegressionMetrics(int iteration, string trainerName, RegressionMetrics metrics)
        {
            Console.WriteLine($"{iteration,-3}{trainerName,-35}{metrics.RSquared,-10:0.###}{metrics.LossFn,-8:0.##}{metrics.L1,-15:#.##}{metrics.L2,-15:#.##}{metrics.Rms,-10:#.##}");
        }

        public static void PrintRegressionMetricsHeader()
        {
            Console.WriteLine($"*************************************************");
            Console.WriteLine($"*       Metrics for regression models     ");
            Console.WriteLine($"*------------------------------------------------");
            Console.WriteLine($"{" ",-3}{"Trainer",-35}{"R2-Score",-10}{"LossFn",-8}{"Absolute-loss",-15}{"Squared-loss",-15}{"RMS-loss",-10}");
            Console.WriteLine();
        }
    }
}
