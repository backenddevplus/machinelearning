﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ML.Auto
{
    internal class BestResultUtil
    {
        public static RunDetails<TMetrics> GetBestRun<TMetrics>(IEnumerable<RunDetails<TMetrics>> results,
            IMetricsAgent<TMetrics> metricsAgent, bool isMetricMaximizing)
        {
            results = results.Where(r => r.ValidationMetrics != null);
            if (!results.Any()) { return null; }
            var scores = results.Select(r => metricsAgent.GetScore(r.ValidationMetrics));
            var indexOfBestScore = GetIndexOfBestScore(scores, isMetricMaximizing);
            return results.ElementAt(indexOfBestScore);
        }

        public static CrossValidationRunDetails<TMetrics> GetBestRun<TMetrics>(IEnumerable<CrossValidationRunDetails<TMetrics>> results,
            IMetricsAgent<TMetrics> metricsAgent, bool isMetricMaximizing)
        {
            results = results.Where(r => r.Results != null && r.Results.Any(x => x.ValidationMetrics != null));
            if (!results.Any()) { return null; }
            var scores = results.Select(r => r.Results.Average(x => metricsAgent.GetScore(x.ValidationMetrics)));
            var indexOfBestScore = GetIndexOfBestScore(scores, isMetricMaximizing);
            return results.ElementAt(indexOfBestScore);
        }

        public static IEnumerable<RunDetails<T>> GetTopNRunResults<T>(IEnumerable<RunDetails<T>> results,
            IMetricsAgent<T> metricsAgent, int n, bool isMetricMaximizing)
        {
            results = results.Where(r => r.ValidationMetrics != null);
            if (!results.Any()) { return null; }

            IEnumerable<RunDetails<T>> orderedResults;
            if (isMetricMaximizing)
            {
                orderedResults = results.OrderByDescending(t => metricsAgent.GetScore(t.ValidationMetrics));
            }
            else
            {
                orderedResults = results.OrderBy(t => metricsAgent.GetScore(t.ValidationMetrics));
            }

            return orderedResults.Take(n);
        }

        public static int GetIndexOfBestScore(IEnumerable<double> scores, bool isMetricMaximizing)
        {
            return isMetricMaximizing ? GetIndexOfMaxScore(scores) : GetIndexOfMinScore(scores);
        }

        private static int GetIndexOfMinScore(IEnumerable<double> scores)
        {
            var minScore = double.PositiveInfinity;
            var minIndex = -1;
            for (var i = 0; i < scores.Count(); i++)
            {
                if (scores.ElementAt(i) < minScore)
                {
                    minScore = scores.ElementAt(i);
                    minIndex = i;
                }
            }
            return minIndex;
        }

        private static int GetIndexOfMaxScore(IEnumerable<double> scores)
        {
            var maxScore = double.NegativeInfinity;
            var maxIndex = -1;
            for (var i = 0; i < scores.Count(); i++)
            {
                if (scores.ElementAt(i) > maxScore)
                {
                    maxScore = scores.ElementAt(i);
                    maxIndex = i;
                }
            }
            return maxIndex;
        }
    }
}
