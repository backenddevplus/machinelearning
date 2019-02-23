﻿using System;
using System.Linq;
using Microsoft.ML.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ML.Auto.Test
{
    [TestClass]
    public class ColumnInferenceTests
    {
        [TestMethod]
        public void UnGroupReturnsMoreColumnsThanGroup()
        {
            var dataPath = DatasetUtil.DownloadUciAdultDataset();
            var context = new MLContext();
            var columnInferenceWithoutGrouping = context.AutoInference().InferColumns(dataPath, DatasetUtil.UciAdultLabel, groupColumns: false);
            foreach (var col in columnInferenceWithoutGrouping.TextLoaderArgs.Columns)
            {
                Assert.IsFalse(col.Source.Length > 1 || col.Source[0].Min != col.Source[0].Max);
            }

            var columnInferenceWithGrouping = context.AutoInference().InferColumns(dataPath, DatasetUtil.UciAdultLabel, groupColumns: true);
            Assert.IsTrue(columnInferenceWithGrouping.TextLoaderArgs.Columns.Count() < columnInferenceWithoutGrouping.TextLoaderArgs.Columns.Count());
        }

        [TestMethod]
        public void IncorrectLabelColumnThrows()
        {
            var dataPath = DatasetUtil.DownloadUciAdultDataset();
            var context = new MLContext();
            Assert.ThrowsException<ArgumentException>(new System.Action(() => context.AutoInference().InferColumns(dataPath, "Junk", groupColumns: false)));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void LabelIndexOutOfBoundsThrows()
        {
            new MLContext().AutoInference().InferColumns(DatasetUtil.DownloadUciAdultDataset(), 100);
        }

        [TestMethod]
        public void IdentifyLabelColumnThroughIndexWithHeader()
        {
            var result = new MLContext().AutoInference().InferColumns(DatasetUtil.DownloadUciAdultDataset(), 14, hasHeader: true);
            Assert.AreEqual(true, result.TextLoaderArgs.HasHeader);
            var labelCol = result.TextLoaderArgs.Columns.First(c => c.Source[0].Min == 14 && c.Source[0].Max == 14);
            Assert.AreEqual("hours-per-week", labelCol.Name);
            Assert.AreEqual("hours-per-week", result.ColumnInformation.LabelColumn);
        }

        [TestMethod]
        public void IdentifyLabelColumnThroughIndexWithoutHeader()
        {
            var result = new MLContext().AutoInference().InferColumns(DatasetUtil.DownloadIrisDataset(), DatasetUtil.IrisDatasetLabelColIndex);
            Assert.AreEqual(false, result.TextLoaderArgs.HasHeader);
            var labelCol = result.TextLoaderArgs.Columns.First(c => c.Source[0].Min == DatasetUtil.IrisDatasetLabelColIndex &&
                c.Source[0].Max == DatasetUtil.IrisDatasetLabelColIndex);
            Assert.AreEqual(DefaultColumnNames.Label, labelCol.Name);
            Assert.AreEqual(DefaultColumnNames.Label, result.ColumnInformation.LabelColumn);
        }

        [TestMethod]
        public void DatasetWithEmptyColumn()
        {
            var result = new MLContext().AutoInference().InferColumns(@".\TestData\DatasetWithEmptyColumn.txt", DefaultColumnNames.Label);
            var emptyColumn = result.TextLoaderArgs.Columns.First(c => c.Name == "Empty");
            Assert.AreEqual(DataKind.TX, emptyColumn.Type);
        }

        [TestMethod]
        public void DatasetWithBoolColumn()
        {
            var result = new MLContext().AutoInference().InferColumns(@".\TestData\BinaryDatasetWithBoolColumn.txt", DefaultColumnNames.Label);
            Assert.AreEqual(2, result.TextLoaderArgs.Columns.Count());

            var boolColumn = result.TextLoaderArgs.Columns.First(c => c.Name == "Bool");
            var labelColumn = result.TextLoaderArgs.Columns.First(c => c.Name == DefaultColumnNames.Label);
            // ensure non-label Boolean column is detected as R4
            Assert.AreEqual(DataKind.R4, boolColumn.Type);
            Assert.AreEqual(DataKind.BL, labelColumn.Type);

            // ensure non-label Boolean column is detected as R4
            Assert.AreEqual(1, result.ColumnInformation.NumericColumns.Count());
            Assert.AreEqual("Bool", result.ColumnInformation.NumericColumns.First());
            Assert.AreEqual(DefaultColumnNames.Label, result.ColumnInformation.LabelColumn);
        }

        [TestMethod]
        public void WhereNameColumnIsOnlyFeature()
        {
            var result = new MLContext().AutoInference().InferColumns(@".\TestData\NameColumnIsOnlyFeatureDataset.txt", DefaultColumnNames.Label);
            Assert.AreEqual(2, result.TextLoaderArgs.Columns.Count());

            var nameColumn = result.TextLoaderArgs.Columns.First(c => c.Name == "Username");
            var labelColumn = result.TextLoaderArgs.Columns.First(c => c.Name == DefaultColumnNames.Label);
            Assert.AreEqual(DataKind.TX, nameColumn.Type);
            Assert.AreEqual(DataKind.BL, labelColumn.Type);
            
            Assert.AreEqual(1, result.ColumnInformation.TextColumns.Count());
            Assert.AreEqual("Username", result.ColumnInformation.TextColumns.First());
            Assert.AreEqual(DefaultColumnNames.Label, result.ColumnInformation.LabelColumn);
        }

        [TestMethod]
        public void DefaultColumnNamesInferredCorrectly()
        {
            var result = new MLContext().AutoInference().InferColumns(@".\TestData\DatasetWithDefaultColumnNames.txt",
                new ColumnInformation()
                {
                    LabelColumn = DefaultColumnNames.Label,
                    WeightColumn = DefaultColumnNames.Weight,
                },
                groupColumns : false);

            Assert.AreEqual(DefaultColumnNames.Label, result.ColumnInformation.LabelColumn);
            Assert.AreEqual(DefaultColumnNames.Weight, result.ColumnInformation.WeightColumn);
            Assert.AreEqual(result.ColumnInformation.NumericColumns.Count(), 3);
        }

        [TestMethod]
        public void DefaultColumnNamesNoGrouping()
        {
            var result = new MLContext().AutoInference().InferColumns(@".\TestData\DatasetWithDefaultColumnNames.txt",
                new ColumnInformation()
                {
                    LabelColumn = DefaultColumnNames.Label,
                    WeightColumn = DefaultColumnNames.Weight,
                });

            Assert.AreEqual(DefaultColumnNames.Label, result.ColumnInformation.LabelColumn);
            Assert.AreEqual(DefaultColumnNames.Weight, result.ColumnInformation.WeightColumn);
            Assert.AreEqual(1, result.ColumnInformation.NumericColumns.Count());
            Assert.AreEqual(DefaultColumnNames.Features, result.ColumnInformation.NumericColumns.First());
        }

        [TestMethod]
        public void InferColumnsColumnInfoParam()
        {
            var columnInfo = new ColumnInformation() { LabelColumn = DatasetUtil.MlNetGeneratedRegressionLabel };
            var result = new MLContext().AutoInference().InferColumns(DatasetUtil.DownloadMlNetGeneratedRegressionDataset(), 
                columnInfo);
            var labelCol = result.TextLoaderArgs.Columns.First(c => c.Name == DatasetUtil.MlNetGeneratedRegressionLabel);
            Assert.AreEqual(DataKind.R4, labelCol.Type);
            Assert.AreEqual(DatasetUtil.MlNetGeneratedRegressionLabel, result.ColumnInformation.LabelColumn);
            Assert.AreEqual(1, result.ColumnInformation.NumericColumns.Count());
            Assert.AreEqual(DefaultColumnNames.Features, result.ColumnInformation.NumericColumns.First());
            Assert.AreEqual(null, result.ColumnInformation.WeightColumn);
        }
    }
}