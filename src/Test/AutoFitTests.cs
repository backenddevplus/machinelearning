// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Data.DataView;
using Microsoft.ML.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Microsoft.ML.Auto.Test
{
    [TestClass]
    public class AutoFitTests
    {
        [TestMethod]
        public void AutoFitBinaryTest()
        {
            var context = new MLContext();
            var dataPath = DatasetUtil.DownloadUciAdultDataset();
            var columnInference = context.Auto().InferColumns(dataPath, DatasetUtil.UciAdultLabel);
            var textLoader = context.Data.CreateTextLoader(columnInference.TextLoaderOptions);
            var trainData = textLoader.Load(dataPath);
            var validationData = context.Data.TakeRows(trainData, 100);
            trainData = context.Data.SkipRows(trainData, 100);
            var results = context.Auto()
                .CreateBinaryClassificationExperiment(0)
                .Execute(trainData, validationData, new ColumnInformation() { LabelColumn = DatasetUtil.UciAdultLabel });
            var best = results.Best();
            Assert.IsTrue(best.ValidationMetrics.Accuracy > 0.80);
            Assert.IsNotNull(best.Estimator);
        }

        [TestMethod]
        public void AutoFitMultiTest()
        {
            var context = new MLContext();
            var columnInference = context.Auto().InferColumns(DatasetUtil.TrivialMulticlassDatasetPath, DatasetUtil.TrivialMulticlassDatasetLabel);
            var textLoader = context.Data.CreateTextLoader(columnInference.TextLoaderOptions);
            var trainData = textLoader.Load(DatasetUtil.TrivialMulticlassDatasetPath);
            var validationData = context.Data.TakeRows(trainData, 20);
            trainData = context.Data.SkipRows(trainData, 20);
            var results = context.Auto()
                .CreateMulticlassClassificationExperiment(0)
                .Execute(trainData, validationData, new ColumnInformation() { LabelColumn = DatasetUtil.TrivialMulticlassDatasetLabel });
            var best = results.Best();
            Assert.IsTrue(best.ValidationMetrics.AccuracyMicro >= 0.8);
            var scoredData = best.Model.Transform(validationData);
            Assert.AreEqual(NumberDataViewType.Single, scoredData.Schema[DefaultColumnNames.PredictedLabel].Type);
        }

        [TestMethod]
        public void AutoFitRegressionTest()
        {
            var context = new MLContext();
            var dataPath = DatasetUtil.DownloadMlNetGeneratedRegressionDataset();
            var columnInference = context.Auto().InferColumns(dataPath, DatasetUtil.MlNetGeneratedRegressionLabel);
            var textLoader = context.Data.CreateTextLoader(columnInference.TextLoaderOptions);
            var trainData = textLoader.Load(dataPath);
            var validationData = context.Data.TakeRows(trainData, 20);
            trainData = context.Data.SkipRows(trainData, 20);
            var results = context.Auto()
                .CreateRegressionExperiment(0)
                .Execute(trainData, validationData,
                    new ColumnInformation() { LabelColumn = DatasetUtil.MlNetGeneratedRegressionLabel });

            Assert.IsTrue(results.Max(i => i.ValidationMetrics.RSquared > 0.9));
        }
    }
}
