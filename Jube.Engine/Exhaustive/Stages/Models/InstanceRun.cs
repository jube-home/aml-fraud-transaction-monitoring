/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Threading;
using Jube.Analytics.Categorical;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Exhaustive.Calibration;
using Jube.Engine.Exhaustive.Storage;
using Jube.Data.Query.GetNextExhaustiveSearchInstanceQuery;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Engine.Exhaustive.Stages.Models
{
    internal class InstanceRun(
        GetNextExhaustiveSearchInstanceQueryDto instance,
        DbContext dbContext,
        CancellationToken token)
    {
        internal GetNextExhaustiveSearchInstanceQueryDto Instance { get; } = instance;
        internal DbContext DbContext { get; } = dbContext;
        internal CancellationToken Token { get; } = token;
        internal ExhaustiveSearchInstanceRepository RepositoryInstance { get; } = new(dbContext);
        internal ExhaustiveSearchInstanceVariableRepository RepositoryVariables { get; } = new(dbContext);

        internal ExhaustiveSearchInstanceVariableHistogramRepository RepositoryVariableHistograms { get; } =
            new(dbContext);

        internal ExhaustiveSearchInstanceVariableMultiCollinearityRepository RepositoryMultiCollinearities { get; } =
            new(dbContext);

        internal bool MockData { get; set; }
        internal DatasetTrainingPlan Dataset { get; set; }
        internal double[][] Data { get; set; }
        internal Dictionary<int, Variable> Variables { get; set; }
        internal double[] Outputs { get; set; }
        internal double[] CloneOfOutputsFromAnomaly { get; set; }
        internal int ClassCount { get; set; }
        internal BaseRateResolution BaseRate { get; set; }
        internal CategoricalPlan CategoricalPlan { get; set; }
        internal double[][] SyntheticData { get; set; }
        internal double[] SyntheticOutputs { get; set; }
        internal double SyntheticWeight { get; set; } = 1;
        internal int BootstrapTargetPerClass { get; set; }
        internal int BootstrapRowsAdded { get; set; }
        internal string BootstrapMethod { get; set; }
    }
}