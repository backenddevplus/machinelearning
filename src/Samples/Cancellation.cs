﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Data.DataView;
using Microsoft.ML;
using Microsoft.ML.Auto;
using Microsoft.ML.Data;

namespace Samples
{
    static class Cancellation
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
            var columnInference = mlContext.AutoInference().InferColumns(TrainDataPath, LabelColumn, ',');

            // STEP 2: Load data
            TextLoader textLoader = mlContext.Data.CreateTextLoader(columnInference.TextLoaderArgs);
            IDataView trainDataView = textLoader.Read(TrainDataPath);
            IDataView testDataView = textLoader.Read(TestDataPath);

            int cancelAfterInSeconds = 20;
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(cancelAfterInSeconds * 1000);

            Stopwatch watch = Stopwatch.StartNew();

            // STEP 3: Auto inference with a cancellation token
            Console.WriteLine($"Invoking new AutoML regression experiment...");
            var runResults = mlContext.AutoInference()
                .CreateRegressionExperiment(new RegressionExperimentSettings()
                {
                    MaxInferenceTimeInSeconds = 60,
                    CancellationToken = cts.Token
                })
                .Execute(trainDataView, LabelColumn);

            Console.WriteLine($"{runResults.Count()} models were returned after {cancelAfterInSeconds} seconds");

            Console.WriteLine("Press any key to continue...");
            Console.ReadLine();
        }
    }
}
