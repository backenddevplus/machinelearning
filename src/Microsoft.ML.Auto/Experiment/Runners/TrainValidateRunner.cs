﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.ML.Auto
{
    internal class TrainValidateRunner<TMetrics> : IRunner<RunDetails<TMetrics>>
        where TMetrics : class
    {
        private readonly MLContext _context;
        private readonly IDataView _trainData;
        private readonly IDataView _validData;
        private readonly string _labelColumn;
        private readonly IMetricsAgent<TMetrics> _metricsAgent;
        private readonly IEstimator<ITransformer> _preFeaturizer;
        private readonly ITransformer _preprocessorTransform;
        private readonly IDebugLogger _logger;
        private readonly DataViewSchema _modelInputSchema;

        public TrainValidateRunner(MLContext context,
            IDataView trainData,
            IDataView validData,
            string labelColumn,
            IMetricsAgent<TMetrics> metricsAgent,
            IEstimator<ITransformer> preFeaturizer,
            ITransformer preprocessorTransform,
            IDebugLogger logger)
        {
            _context = context;
            _trainData = trainData;
            _validData = validData;
            _labelColumn = labelColumn;
            _metricsAgent = metricsAgent;
            _preFeaturizer = preFeaturizer;
            _preprocessorTransform = preprocessorTransform;
            _logger = logger;
            _modelInputSchema = trainData.Schema;
        }

        public (SuggestedPipelineRunDetails suggestedPipelineRunDetails, RunDetails<TMetrics> runDetails) 
            Run(SuggestedPipeline pipeline, DirectoryInfo modelDirectory, int iterationNum)
        {
            var modelFileInfo = GetModelFileInfo(modelDirectory, iterationNum);
            var trainResult = RunnerUtil.TrainAndScorePipeline(_context, pipeline, _trainData, _validData,
                _labelColumn, _metricsAgent, _preprocessorTransform, modelFileInfo, _modelInputSchema, _logger);
            var suggestedPipelineRunDetails = new SuggestedPipelineRunDetails<TMetrics>(pipeline,
                trainResult.score,
                trainResult.exception == null,
                trainResult.metrics,
                trainResult.model,
                trainResult.exception);
            var runDetails = suggestedPipelineRunDetails.ToIterationResult(_preFeaturizer);
            return (suggestedPipelineRunDetails, runDetails);
        }

        private static FileInfo GetModelFileInfo(DirectoryInfo modelDirectory, int iterationNum)
        {
            return modelDirectory == null ? 
                null :
                new FileInfo(Path.Combine(modelDirectory.FullName, $"Model{iterationNum}.zip"));
        }
    }
}