﻿using System.IO;
using Microsoft.ML.Auto;

namespace Microsoft.ML.CLI.CodeGenerator.CSharp
{
    internal class CodeGeneratorSettings
    {
        internal string LabelName { get; set; }

        internal FileInfo ModelPath { get; set; }

        internal string OutputName { get; set; }

        internal string OutputBaseDir { get; set; }

        internal FileInfo TrainDataset { get; set; }

        internal FileInfo TestDataset { get; set; }

        internal TaskKind MlTask { get; set; }

    }
}
