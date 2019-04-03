﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.ML.Auto
{
    internal class CrossValRunner<TMetrics> : IRunner<CrossValidationRunDetails<TMetrics>>
        where TMetrics : class
    {
        private readonly MLContext _context;
        private readonly IDataView[] _trainDatasets;
        private readonly IDataView[] _validDatasets;
        private readonly IMetricsAgent<TMetrics> _metricsAgent;
        private readonly IEstimator<ITransformer> _preFeaturizer;
        private readonly ITransformer[] _preprocessorTransforms;
        private readonly string _labelColumn;
        private readonly IDebugLogger _logger;
        private readonly DataViewSchema _modelInputSchema;

        public CrossValRunner(MLContext context,
            IDataView[] trainDatasets,
            IDataView[] validDatasets,
            IMetricsAgent<TMetrics> metricsAgent,
            IEstimator<ITransformer> preFeaturizer,
            string labelColumn,
            IDebugLogger logger)
        {
            _context = context;
            _trainDatasets = trainDatasets;
            _validDatasets = validDatasets;
            _metricsAgent = metricsAgent;
            _preFeaturizer = preFeaturizer;
            _labelColumn = labelColumn;
            _logger = logger;
            _modelInputSchema = trainDatasets[0].Schema;

            if (_preFeaturizer != null)
            {
                _preprocessorTransforms = new ITransformer[_trainDatasets.Length];
                for (var i = 0; i < _trainDatasets.Length; i++)
                {
                    // Preprocess train and validation data
                    _preprocessorTransforms[i] = _preFeaturizer.Fit(_trainDatasets[i]);
                    _trainDatasets[i] = _preprocessorTransforms[i].Transform(_trainDatasets[i]);
                    _validDatasets[i] = _preprocessorTransforms[i].Transform(_validDatasets[i]);
                }
            }
        }

        public (SuggestedPipelineRunDetails suggestedPipelineRunDetails, CrossValidationRunDetails<TMetrics> runDetails) 
            Run(SuggestedPipeline pipeline, DirectoryInfo modelDirectory, int iterationNum)
        {
            var trainResults = new List<SuggestedPipelineTrainResult<TMetrics>>();

            for (var i = 0; i < _trainDatasets.Length; i++)
            {
                var modelFileInfo = RunnerUtil.GetModelFileInfo(modelDirectory, iterationNum, i + 1);
                var trainResult = RunnerUtil.TrainAndScorePipeline(_context, pipeline, _trainDatasets[i], _validDatasets[i],
                    _labelColumn, _metricsAgent, _preFeaturizer, _preprocessorTransforms?[i], modelFileInfo, _modelInputSchema, _logger);
                trainResults.Add(new SuggestedPipelineTrainResult<TMetrics>(trainResult.model, trainResult.metrics, trainResult.exception, trainResult.score));
            }

            var avgScore = CalcAverageScore(trainResults.Select(r => r.Score));
            var allRunsSucceeded = trainResults.All(r => r.Exception == null);

            var suggestedPipelineRunDetails = new SuggestedPipelineCrossValRunDetails<TMetrics>(pipeline, avgScore, allRunsSucceeded, trainResults);
            var runDetails = suggestedPipelineRunDetails.ToIterationResult();
            return (suggestedPipelineRunDetails, runDetails);
        }

        private static double CalcAverageScore(IEnumerable<double> scores)
        {
            if (scores.Any(s => double.IsNaN(s)))
            {
                return double.NaN;
            }
            return scores.Average();
        }
    }
}
