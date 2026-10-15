/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.Engine.Integrity.Models;

namespace Jube.Service.Query.EntityAnalysisModelIntegrity
{
    internal static class IntegrityCodeText
    {
        public static string Describe(IntegrityCode code)
        {
            return code switch
            {
                IntegrityCode.DependencyDangling => "Uses a name that does not exist",
                IntegrityCode.DependencyOnInactive => "Uses an inactive entity",
                IntegrityCode.DependencyOnUnapproved => "Uses an entity that is not yet approved",
                IntegrityCode.EntityPendingApproval => "Changed and waiting for approval",
                IntegrityCode.EntityUnreferenced => "Not used by anything",
                IntegrityCode.EngineCompileFailed => "Did not compile in the engine",
                IntegrityCode.ModelInactive => "Model is inactive",
                IntegrityCode.NoActiveActivationRules => "No active activation rules",
                IntegrityCode.TtlCountersDisabled => "TTL counters are disabled",
                IntegrityCode.SearchKeyTtlShortensWindow =>
                    "Search key history is shorter than the rule window",
                IntegrityCode.SearchKeyTtlCollapsesWindow =>
                    "Search key keeps no history for the rule window",
                IntegrityCode.EngineStateUnavailable => "Engine state is unavailable",
                IntegrityCode.EngineNoNodes => "No engine instance has synchronised",
                IntegrityCode.EngineNodeStale => "Engine instance has stopped reporting",
                IntegrityCode.EngineModelNotLoaded => "Model is not loaded by an engine instance",
                IntegrityCode.EngineModelNotStarted => "Model is loaded but not started",
                IntegrityCode.EngineEntityNotLoaded => "Not loaded by an engine instance",
                IntegrityCode.EngineEntityStale =>
                    "Engine still runs an entity that is no longer active",
                _ => code.ToString()
            };
        }
    }
}