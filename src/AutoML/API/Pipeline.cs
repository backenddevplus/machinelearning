﻿using System.Collections.Generic;
using Microsoft.ML.Core.Data;

namespace Microsoft.ML.Auto
{
    public class Pipeline
    {
        public PipelineNode[] Elements { get; set; }

        public Pipeline(PipelineNode[] elements)
        {
            Elements = elements;
        }

        // (used by Newtonsoft)
        internal Pipeline()
        {
        }
        
        public IEstimator<ITransformer> ToEstimator()
        {
            var inferredPipeline = InferredPipeline.FromPipeline(this);
            return inferredPipeline.ToEstimator();
        }
    }

    public class PipelineNode
    {
        public string Name { get; set; }
        public PipelineNodeType ElementType { get; set; }
        public string[] InColumns { get; set; }
        public string[] OutColumns { get; set; }
        public IDictionary<string, object> Properties { get; set; }

        public PipelineNode(string name, PipelineNodeType elementType,
            string[] inColumns, string[] outColumns,
            IDictionary<string, object> properties = null)
        {
            Name = name;
            ElementType = elementType;
            InColumns = inColumns;
            OutColumns = outColumns;
            Properties = properties ?? new Dictionary<string, object>();
        }

        public PipelineNode(string name, PipelineNodeType elementType, 
            string inColumn, string outColumn, IDictionary<string, object> properties = null) :
            this(name, elementType, new string[] { inColumn }, new string[] { outColumn }, properties)
        {
        }

        public PipelineNode(string name, PipelineNodeType elementType,
            string[] inColumns, string outColumn, IDictionary<string, object> properties = null) :
            this(name, elementType, inColumns, new string[] { outColumn }, properties)
        {
        }

        // (used by Newtonsoft)
        internal PipelineNode()
        {
        }
    }

    public enum PipelineNodeType
    {
        Transform,
        Trainer
    }

    public class CustomProperty
    {
        public readonly string Name;
        public readonly IDictionary<string, object> Properties;
    }

    public class PipelineRunResult
    {
        public readonly Pipeline Pipeline;
        public readonly double Score;

        /// <summary>
        /// This setting is true if the pipeline run succeeded & ran to completion.
        /// Else, it is false if some exception was thrown before the run could complete.
        /// </summary>
        public readonly bool RunSucceded;

        public PipelineRunResult(Pipeline pipeline, double score, bool runSucceeded)
        {
            Pipeline = pipeline;
            Score = score;
            RunSucceded = runSucceeded;
        }
    }
}
