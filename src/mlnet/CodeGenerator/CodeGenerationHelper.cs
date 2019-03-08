﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.Data.DataView;
using Microsoft.ML.Auto;
using Microsoft.ML.CLI.CodeGenerator.CSharp;
using Microsoft.ML.CLI.Data;
using Microsoft.ML.CLI.Utilities;
using Microsoft.ML.Data;
using NLog;

namespace Microsoft.ML.CLI.CodeGenerator
{
    internal class CodeGenerationHelper
    {

        private IAutoMLEngine automlEngine;
        private NewCommandSettings settings;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private TaskKind taskKind;
        public CodeGenerationHelper(IAutoMLEngine automlEngine, NewCommandSettings settings)
        {
            this.automlEngine = automlEngine;
            this.settings = settings;
            this.taskKind = Utils.GetTaskKind(settings.MlTask);
        }

        public void GenerateCode()
        {
            var context = new MLContext();

            // Infer columns
            ColumnInferenceResults columnInference = null;
            try
            {
                columnInference = automlEngine.InferColumns(context);
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Error, $"{Strings.InferColumnError}");
                logger.Log(LogLevel.Error, e.Message);
                logger.Log(LogLevel.Debug, e.ToString());
                logger.Log(LogLevel.Error, Strings.Exiting);
                return;
            }

            // Sanitize columns
            Array.ForEach(columnInference.TextLoaderOptions.Columns, t => t.Name = Utils.Sanitize(t.Name));

            var sanitizedLabelName = Utils.Sanitize(columnInference.ColumnInformation.LabelColumn);

            // Load data
            (IDataView trainData, IDataView validationData) = LoadData(context, columnInference.TextLoaderOptions);

            // Explore the models
            (Pipeline, ITransformer) result = default;
            Console.WriteLine($"{Strings.ExplorePipeline}: {settings.MlTask}");
            try
            {
                result = automlEngine.ExploreModels(context, trainData, validationData, sanitizedLabelName);
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Error, $"{Strings.ExplorePipelineException}:");
                logger.Log(LogLevel.Error, e.Message);
                logger.Log(LogLevel.Debug, e.ToString());
                logger.Log(LogLevel.Error, Strings.Exiting);
                return;
            }

            //Get the best pipeline
            Pipeline pipeline = null;
            pipeline = result.Item1;
            var model = result.Item2;

            // Save the model
            logger.Log(LogLevel.Info, Strings.SavingBestModel);
            var modelPath = new FileInfo(Path.Combine(settings.OutputPath.FullName, "model.zip"));
            Utils.SaveModel(model, modelPath, context);

            // Generate the Project
            GenerateProject(columnInference, pipeline, sanitizedLabelName, modelPath);
        }

        internal void GenerateProject(ColumnInferenceResults columnInference, Pipeline pipeline, string labelName, FileInfo modelPath)
        {
            //Generate code
            logger.Log(LogLevel.Info, $"{Strings.GenerateProject} : {settings.OutputPath.FullName}");
            var codeGenerator = new CodeGenerator.CSharp.CodeGenerator(
                pipeline,
                columnInference,
                new CodeGeneratorSettings()
                {
                    TrainDataset = settings.Dataset.FullName,
                    MlTask = taskKind,
                    TestDataset = settings.TestDataset?.FullName,
                    OutputName = settings.Name,
                    OutputBaseDir = settings.OutputPath.FullName,
                    LabelName = labelName,
                    ModelPath = modelPath.FullName
                });
            codeGenerator.GenerateOutput();
        }

        internal (IDataView, IDataView) LoadData(MLContext context, TextLoader.Options textLoaderOptions)
        {
            logger.Log(LogLevel.Info, Strings.CreateDataLoader);
            var textLoader = context.Data.CreateTextLoader(textLoaderOptions);

            logger.Log(LogLevel.Info, Strings.LoadData);
            var trainData = textLoader.Load(settings.Dataset.FullName);
            var validationData = settings.ValidationDataset == null ? null : textLoader.Load(settings.ValidationDataset.FullName);

            return (trainData, validationData);
        }
    }
}
