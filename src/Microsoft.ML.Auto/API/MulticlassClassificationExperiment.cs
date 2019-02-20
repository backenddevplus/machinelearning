﻿// Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.DataView;
using Microsoft.ML.Core.Data;
using Microsoft.ML.Data;

namespace Microsoft.ML.Auto
{
    public class MulticlassExperimentSettings : ExperimentSettings
    {
        public IProgress<RunResult<MultiClassClassifierMetrics>> ProgressCallback;
        public MulticlassClassificationMetric OptimizingMetric;
        public MulticlassClassificationTrainer[] WhitelistedTrainers;
    }

    public enum MulticlassClassificationMetric
    {
        Accuracy
    }

    public enum MulticlassClassificationTrainer
    {
        LightGbm
    }

    public class MulticlassClassificationExperiment
    {
        private readonly MLContext _context;
        private readonly MulticlassExperimentSettings _settings;

        internal MulticlassClassificationExperiment(MLContext context, MulticlassExperimentSettings settings)
        {
            _context = context;
            _settings = settings;
        }

        public IEnumerable<RunResult<MultiClassClassifierMetrics>> Execute(IDataView trainData, ColumnInformation columnInformation = null, IEstimator<ITransformer> preFeaturizers = null)
        {
            return Execute(_context, trainData, columnInformation, null, preFeaturizers);
        }

        public IEnumerable<RunResult<MultiClassClassifierMetrics>> Execute(IDataView trainData, IDataView validationData, ColumnInformation columnInformation = null, IEstimator<ITransformer> preFeaturizers = null)
        {
            return Execute(_context, trainData, columnInformation, validationData, preFeaturizers);
        }

        internal RunResult<BinaryClassificationMetrics> Execute(IDataView trainData, uint numberOfCVFolds, ColumnInformation columnInformation = null, IEstimator<ITransformer> preFeaturizers = null)
        {
            throw new NotImplementedException();
        }

        internal IEnumerable<RunResult<MultiClassClassifierMetrics>> Execute(MLContext context,
            IDataView trainData,
            ColumnInformation columnInfo,
            IDataView validationData = null,
            IEstimator<ITransformer> preFeaturizers = null)
        {
            columnInfo = columnInfo ?? new ColumnInformation();
            //UserInputValidationUtil.ValidateAutoFitArgs(trainData, labelColunName, validationData, settings, columnPurposes)

            // run autofit & get all pipelines run in that process
            var autoFitter = new AutoFitter<MultiClassClassifierMetrics>(context, TaskKind.MulticlassClassification, trainData, 
                columnInfo, validationData, preFeaturizers, OptimizingMetric.Accuracy, 
                _settings?.ProgressCallback, _settings);

            return autoFitter.Fit();
        }
    }

    public static class MulticlassExperimentResultExtensions
    {
        public static RunResult<MultiClassClassifierMetrics> Best(this IEnumerable<RunResult<MultiClassClassifierMetrics>> results)
        {
            double maxScore = results.Select(r => r.Metrics.AccuracyMicro).Max();
            return results.First(r => r.Metrics.AccuracyMicro == maxScore);
        }
    }
}
