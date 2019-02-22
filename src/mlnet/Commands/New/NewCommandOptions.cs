// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.ML.CLI.Data
{
    public class NewCommandOptions
    {
        public string Name { get; set; }

        public FileInfo Dataset { get; set; }

        public FileInfo ValidationDataset { get; set; }

        public FileInfo TestDataset { get; set; }

        public string LabelColumnName { get; set; }

        public string Verbosity { get; set; }

        public uint LabelColumnIndex { get; set; }

        public string MlTask { get; set; }

        public uint MaxExplorationTime { get; set; }

        public DirectoryInfo OutputPath { get; set; }

        public bool HasHeader { get; set; }

    }
}
