// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.DataView;
using Microsoft.ML.Data;

namespace Microsoft.ML.Auto
{
    internal static class PipelineSuggester
    {
        private const int TopKTrainers = 3;

        public static Pipeline GetNextPipeline(MLContext context,
            IEnumerable<PipelineScore> history,
            (string, DataViewType, ColumnPurpose, ColumnDimensions)[] columns,
            TaskKind task,
            bool isMaximizingMetric = true)
        {
            var inferredHistory = history.Select(r => SuggestedPipelineResult.FromPipelineRunResult(context, r));
            var nextInferredPipeline = GetNextInferredPipeline(context, inferredHistory, columns, task, isMaximizingMetric);
            return nextInferredPipeline?.ToPipeline();
        }

        public static SuggestedPipeline GetNextInferredPipeline(MLContext context,
            IEnumerable<SuggestedPipelineResult> history,
            (string, DataViewType, ColumnPurpose, ColumnDimensions)[] columns,
            TaskKind task,
            bool isMaximizingMetric,
            IEnumerable<TrainerName> trainerWhitelist = null,
            bool? _enableCaching = null)
        {
            var availableTrainers = RecipeInference.AllowedTrainers(context, task, 
                ColumnInformationUtil.BuildColumnInfo(columns), trainerWhitelist);
            var transforms = CalculateTransforms(context, columns, task);
            //var transforms = TransformInferenceApi.InferTransforms(context, columns, task);

            // if we haven't run all pipelines once
            if (history.Count() < availableTrainers.Count())
            {
                return GetNextFirstStagePipeline(context, history, availableTrainers, transforms, _enableCaching);
            }

            // get top trainers from stage 1 runs
            var topTrainers = GetTopTrainers(history, availableTrainers, isMaximizingMetric);

            // sort top trainers by # of times they've been run, from lowest to highest
            var orderedTopTrainers = OrderTrainersByNumTrials(history, topTrainers);

            // keep as hashset of previously visited pipelines
            var visitedPipelines = new HashSet<SuggestedPipeline>(history.Select(h => h.Pipeline));

            // iterate over top trainers (from least run to most run),
            // to find next pipeline
            foreach (var trainer in orderedTopTrainers)
            {
                var newTrainer = trainer.Clone();

                // repeat until passes or runs out of chances
                const int maxNumberAttempts = 10;
                var count = 0;
                do
                {
                    // sample new hyperparameters for the learner
                    if (!SampleHyperparameters(context, newTrainer, history, isMaximizingMetric))
                    {
                        // if unable to sample new hyperparameters for the learner
                        // (ie SMAC returned 0 suggestions), break
                        break;
                    }

                    var suggestedPipeline = new SuggestedPipeline(transforms, newTrainer, context, _enableCaching);

                    // make sure we have not seen pipeline before
                    if (!visitedPipelines.Contains(suggestedPipeline))
                    {
                        return suggestedPipeline;
                    }
                } while (++count <= maxNumberAttempts);
            }

            return null;
        }
        
        /// <summary>
        /// Get top trainers from first stage
        /// </summary>
        private static IEnumerable<SuggestedTrainer> GetTopTrainers(IEnumerable<SuggestedPipelineResult> history, 
            IEnumerable<SuggestedTrainer> availableTrainers,
            bool isMaximizingMetric)
        {
            // narrow history to first stage runs
            history = history.Take(availableTrainers.Count());

            history = history.GroupBy(r => r.Pipeline.Trainer.TrainerName).Select(g => g.First());
            IEnumerable<SuggestedPipelineResult> sortedHistory = history.OrderBy(r => r.Score);
            if(isMaximizingMetric)
            {
                sortedHistory = sortedHistory.Reverse();
            }
            var topTrainers = sortedHistory.Take(TopKTrainers).Select(r => r.Pipeline.Trainer);
            return topTrainers;
        }

        private static IEnumerable<SuggestedTrainer> OrderTrainersByNumTrials(IEnumerable<SuggestedPipelineResult> history,
            IEnumerable<SuggestedTrainer> selectedTrainers)
        {
            var selectedTrainerNames = new HashSet<TrainerName>(selectedTrainers.Select(t => t.TrainerName));
            return history.Where(h => selectedTrainerNames.Contains(h.Pipeline.Trainer.TrainerName))
                .GroupBy(h => h.Pipeline.Trainer.TrainerName)
                .OrderBy(x => x.Count())
                .Select(x => x.First().Pipeline.Trainer);
        }

        private static SuggestedPipeline GetNextFirstStagePipeline(MLContext context,
            IEnumerable<SuggestedPipelineResult> history,
            IEnumerable<SuggestedTrainer> availableTrainers,
            IEnumerable<SuggestedTransform> transforms,
            bool? _enableCaching)
        {
            var trainer = availableTrainers.ElementAt(history.Count());
            return new SuggestedPipeline(transforms, trainer, context, _enableCaching);
        }

        private static IValueGenerator[] ConvertToValueGenerators(IEnumerable<SweepableParam> hps)
        {
            var results = new IValueGenerator[hps.Count()];

            for (int i = 0; i < hps.Count(); i++)
            {
                switch (hps.ElementAt(i))
                {
                    case SweepableDiscreteParam dp:
                        var dpArgs = new DiscreteParamArguments()
                        {
                            Name = dp.Name,
                            Values = dp.Options.Select(o => o.ToString()).ToArray()
                        };
                        results[i] = new DiscreteValueGenerator(dpArgs);
                        break;

                    case SweepableFloatParam fp:
                        var fpArgs = new FloatParamArguments()
                        {
                            Name = fp.Name,
                            Min = fp.Min,
                            Max = fp.Max,
                            LogBase = fp.IsLogScale,
                        };
                        if (fp.NumSteps.HasValue)
                        {
                            fpArgs.NumSteps = fp.NumSteps.Value;
                        }
                        if (fp.StepSize.HasValue)
                        {
                            fpArgs.StepSize = fp.StepSize.Value;
                        }
                        results[i] = new FloatValueGenerator(fpArgs);
                        break;

                    case SweepableLongParam lp:
                        var lpArgs = new LongParamArguments()
                        {
                            Name = lp.Name,
                            Min = lp.Min,
                            Max = lp.Max,
                            LogBase = lp.IsLogScale
                        };
                        if (lp.NumSteps.HasValue)
                        {
                            lpArgs.NumSteps = lp.NumSteps.Value;
                        }
                        if (lp.StepSize.HasValue)
                        {
                            lpArgs.StepSize = lp.StepSize.Value;
                        }
                        results[i] = new LongValueGenerator(lpArgs);
                        break;
                }
            }
            return results;
        }

        /// <summary>
        /// Samples new hyperparameters for the trainer, and sets them.
        /// Returns true if success (new hyperparams were suggested and set). Else, returns false.
        /// </summary>
        private static bool SampleHyperparameters(MLContext context, SuggestedTrainer trainer, IEnumerable<SuggestedPipelineResult> history, bool isMaximizingMetric)
        {
            var sps = ConvertToValueGenerators(trainer.SweepParams);
            var sweeper = new SmacSweeper(context,
                new SmacSweeper.Arguments
                {
                    SweptParameters = sps
                });

            IEnumerable<SuggestedPipelineResult> historyToUse = history
                .Where(r => r.RunSucceded && r.Pipeline.Trainer.TrainerName == trainer.TrainerName && r.Pipeline.Trainer.HyperParamSet != null && r.Pipeline.Trainer.HyperParamSet.Any());

            // get new set of hyperparameter values
            var proposedParamSet = sweeper.ProposeSweeps(1, historyToUse.Select(h => h.ToRunResult(isMaximizingMetric))).First();
            if(!proposedParamSet.Any())
            {
                return false;
            }

            // associate proposed param set with trainer, so that smart hyperparam
            // sweepers (like KDO) can map them back.
            trainer.SetHyperparamValues(proposedParamSet);

            return true;
        }

        private static IEnumerable<SuggestedTransform> CalculateTransforms(
            MLContext context,
            (string, DataViewType, ColumnPurpose, ColumnDimensions)[] columns,
            TaskKind task)
        {
            var transforms = TransformInferenceApi.InferTransforms(context, columns).ToList();
            // this is a work-around for ML.NET bug tracked by https://github.com/dotnet/machinelearning/issues/1969
            if (task == TaskKind.MulticlassClassification)
            {
                var labelColumn = columns.First(c => c.Item3 == ColumnPurpose.Label).Item1;
                var transform = ValueToKeyMappingExtension.CreateSuggestedTransform(context, labelColumn, labelColumn);
                transforms.Add(transform);
            }
            return transforms;
        }
    }
}