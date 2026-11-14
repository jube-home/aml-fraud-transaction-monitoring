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

namespace Jube.Engine.EntityAnalysisModelInvoke.Extraction
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models;

    public static class FastPathEligibility
    {
        private static readonly object Ineligible = new();

        private static readonly ConditionalWeakTable<ModelSnapshot, object> Decisions = new();

        public static FastPathPlan PlanFor(ModelSnapshot snapshot,
            EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel model)
        {
            if (snapshot == null || model?.Flags == null || model.Flags.EnablePayloadJObject)
            {
                return null;
            }

            if (Decisions.TryGetValue(snapshot, out var cached))
            {
                return cached as FastPathPlan;
            }

            var xPaths = (snapshot.EntityAnalysisModelRequestXPaths ?? []).Select(x => x.XPath).ToList();

            if (model.References != null)
            {
                xPaths.Add(model.References.EntryXPath);

                if (model.References.ReferenceDatePayloadLocationTypeId != RequestFieldExtraction.ReferenceDateFromClock)
                {
                    xPaths.Add(model.References.ReferenceDateXpath);
                }
            }

            var plan = BuildPlan(xPaths);

            Decisions.AddOrUpdate(snapshot, plan ?? Ineligible);

            return plan;
        }

        public static FastPathPlan BuildPlan(IEnumerable<string> xPaths)
        {
            return FastPathPlan.TryBuild(xPaths);
        }
    }
}
