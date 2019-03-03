﻿// Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.ML.Auto
{
    public class ExperimentSettings
    {
        public uint MaxExperimentTimeInSeconds { get; set; } = 24 * 60 * 60;
        public CancellationToken CancellationToken { get; set; } = default;

        internal bool EnableCaching;
        internal int MaxModels = int.MaxValue;
        internal IDebugLogger DebugLogger;
    }
}
