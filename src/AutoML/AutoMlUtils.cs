// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.DataView;
using Microsoft.ML.Transforms;

namespace Microsoft.ML.Auto
{
    internal static class AutoMlUtils
    {
        public static Random Random = new Random();

        public static void Assert(bool boolVal, string message = null)
        {
            if(!boolVal)
            {
                message = message ?? "Assertion failed";
                throw new InvalidOperationException(message);
            }
        }

        public static IDataView Take(this IDataView data, int count)
        {
            var context = new MLContext();
            return TakeFilter.Create(context, data, count);
        }

        public static IDataView DropLastColumn(this IDataView data)
        {
            return new MLContext().Transforms.DropColumns(data.Schema[data.Schema.Count - 1].Name).Fit(data).Transform(data);
        }

        public static (IDataView testData, IDataView validationData) TestValidateSplit(this TrainCatalogBase catalog, IDataView trainData)
        {
            IDataView validationData;
            (trainData, validationData) = catalog.TrainTestSplit(trainData);
            trainData = trainData.DropLastColumn();
            validationData = validationData.DropLastColumn();
            return (trainData, validationData);
        }

        public static IDataView Skip(this IDataView data, int count)
        {
            var context = new MLContext();
            return SkipFilter.Create(context, data, count);
        }

        public static (string, ColumnType, ColumnPurpose, ColumnDimensions)[] GetColumnInfoTuples(MLContext context,
            IDataView data, string label, IDictionary<string, ColumnPurpose> purposeOverrides)
        {
            var purposes = PurposeInference.InferPurposes(context, data, label, purposeOverrides);
            var colDimensions = DatasetDimensionsApi.CalcColumnDimensions(data, purposes);
            var cols = new (string, ColumnType, ColumnPurpose, ColumnDimensions)[data.Schema.Count];
            for (var i = 0; i < cols.Length; i++)
            {
                var schemaCol = data.Schema[i];
                var col = (schemaCol.Name, schemaCol.Type, purposes[i].Purpose, colDimensions[i]);
                cols[i] = col;
            }
            return cols;
        }
    }
}