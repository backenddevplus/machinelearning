﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Data.DataView;
using Microsoft.ML.Core.Data;
using Microsoft.ML.Data;

namespace Microsoft.ML.Auto
{
    public class AutoFitBinaryClassificationOptions
    {
        public IDataView TrainData;
        public string LabelColumnName = DefaultColumnNames.Label;
        public IDataView ValidationData;
        public uint TimeoutInSeconds = AutoFitDefaults.TimeoutInSeconds;
        public CancellationToken CancellationToken = default;
        public IProgress<AutoFitRunResult<BinaryClassificationMetrics>> ProgressCallback;
        public IEstimator<ITransformer> PreFeaturizers;
        public IEnumerable<(string, ColumnPurpose)> ColumnPurposes;
    }

    public static class BinaryClassificationExtensions
    {
        public static List<AutoFitRunResult<BinaryClassificationMetrics>> AutoFit(this BinaryClassificationCatalog catalog,
            IDataView trainData,
            string labelColumnName = DefaultColumnNames.Label,
            IDataView validationData = null,
            uint timeoutInSeconds = AutoFitDefaults.TimeoutInSeconds,
            CancellationToken cancellationToken = default,
            IProgress<AutoFitRunResult<BinaryClassificationMetrics>> progressCallback = null)
        {
            var settings = new AutoFitSettings();
            settings.StoppingCriteria.TimeoutInSeconds = timeoutInSeconds;

            return AutoFit(catalog, trainData, labelColumnName, validationData, settings,
                null, null, cancellationToken, progressCallback, null);
        }

        public static List<AutoFitRunResult<BinaryClassificationMetrics>> AutoFit(this BinaryClassificationCatalog catalog,
                AutoFitBinaryClassificationOptions options)
        {
            var settings = new AutoFitSettings();
            settings.StoppingCriteria.TimeoutInSeconds = options.TimeoutInSeconds;

            return AutoFit(catalog, options.TrainData, options.LabelColumnName, options.ValidationData, settings,
                options.PreFeaturizers, options.ColumnPurposes, options.CancellationToken, options.ProgressCallback, null);
        }

        internal static List<AutoFitRunResult<BinaryClassificationMetrics>> AutoFit(this BinaryClassificationCatalog catalog,
            IDataView trainData,
            string labelColumnName = DefaultColumnNames.Label,
            IDataView validationData = null,
            AutoFitSettings settings = null,
            IEstimator<ITransformer> preFeaturizers = null,
            IEnumerable<(string, ColumnPurpose)> columnPurposes = null,
            CancellationToken cancellationToken = default,
            IProgress<AutoFitRunResult<BinaryClassificationMetrics>> progressCallback = null,
            IDebugLogger debugLogger = null)
        {
            UserInputValidationUtil.ValidateAutoFitArgs(trainData, labelColumnName, validationData, settings, columnPurposes);

            if (validationData == null)
            {
                (trainData, validationData) = catalog.TestValidateSplit(trainData);
            }

            // run autofit & get all pipelines run in that process
            var autoFitter = new AutoFitter<BinaryClassificationMetrics>(TaskKind.BinaryClassification, trainData, labelColumnName, validationData,
                settings, preFeaturizers, columnPurposes,
                OptimizingMetric.RSquared, cancellationToken, progressCallback, debugLogger);

            return autoFitter.Fit();
        }

        public static AutoFitRunResult<BinaryClassificationMetrics> Best(this IEnumerable<AutoFitRunResult<BinaryClassificationMetrics>> results)
        {
            double maxScore = results.Select(r => r.Metrics.Accuracy).Max();
            return results.First(r => r.Metrics.Accuracy == maxScore);
        }
    }

}
