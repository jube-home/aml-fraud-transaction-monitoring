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

namespace Jube.Engine.Integrity
{
    using System.ComponentModel;

    public enum IntegrityCode
    {
        [Description("Uses a name that does not exist")]
        DependencyDangling,

        [Description("Uses an inactive entity")]
        DependencyOnInactive,
        [Description("Not used by anything")] EntityUnreferenced,

        [Description("Did not compile in the engine")]
        EngineCompileFailed,
        [Description("Model is inactive")] ModelInactive,

        [Description("No active activation rules")]
        NoActiveActivationRules,

        [Description("TTL counters are disabled")]
        TtlCountersDisabled,

        [Description("Search key history is shorter than the rule window")]
        SearchKeyTtlShortensWindow,

        [Description("Search key keeps no history for the rule window")]
        SearchKeyTtlCollapsesWindow,

        [Description("Engine state is unavailable")]
        EngineStateUnavailable,

        [Description("No engine instance has synchronised")]
        EngineNoNodes,

        [Description("Engine instance has stopped reporting")]
        EngineNodeStale,

        [Description("Model is not loaded by an engine instance")]
        EngineModelNotLoaded,

        [Description("Model is loaded but not started")]
        EngineModelNotStarted,

        [Description("Not loaded by an engine instance")]
        EngineEntityNotLoaded,

        [Description("Engine still runs an entity that is no longer active")]
        EngineEntityStale
    }
}