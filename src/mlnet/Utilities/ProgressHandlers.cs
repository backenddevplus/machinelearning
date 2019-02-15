﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.ML.Auto;
using Microsoft.ML.Data;

namespace Microsoft.ML.CLI.Utilities
{
    internal class ProgressHandlers
    {
        internal class RegressionHandler : IProgress<AutoFitRunResult<RegressionMetrics>>
        {
            int iterationIndex;
            public RegressionHandler()
            {
                ConsolePrinter.PrintRegressionMetricsHeader();
            }

            public void Report(AutoFitRunResult<RegressionMetrics> iterationResult)
            {
                iterationIndex++;
                ConsolePrinter.PrintRegressionMetrics(iterationIndex, iterationResult.TrainerName, iterationResult.Metrics);
            }
        }

        internal class BinaryClassificationHandler : IProgress<AutoFitRunResult<BinaryClassificationMetrics>>
        {
            int iterationIndex;
            internal BinaryClassificationHandler()
            {
                ConsolePrinter.PrintBinaryClassificationMetricsHeader();
            }

            public void Report(AutoFitRunResult<BinaryClassificationMetrics> iterationResult)
            {
                iterationIndex++;
                ConsolePrinter.PrintBinaryClassificationMetrics(iterationIndex, iterationResult.TrainerName, iterationResult.Metrics);
            }
        }
    }
}
