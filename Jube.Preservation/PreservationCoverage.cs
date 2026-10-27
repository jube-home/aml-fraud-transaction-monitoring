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

using Jube.Data.Poco;

namespace Jube.Preservation
{
    public static class PreservationCoverage
    {
        public static readonly string[] CoveredPrefixes =
            ["ExhaustiveSearchInstance", "ExhaustiveInstance", "AdvancedAnalytics"];

        public static readonly IReadOnlyDictionary<Type, string> Excluded = new Dictionary<Type, string>
        {
            [typeof(AdvancedAnalyticsCache)] =
                "Transient. An expiring pointer store of packed input arrays: rows expire within the configured time to live, " +
                "are rebuilt on demand from the source data and are not a definition of the tenant's models.",
            [typeof(ExhaustiveSearchInstanceVersion)] =
                "Audit history. A copy of the instance row taken on each update, in the same way as the other Version tables, " +
                "which the export does not carry either.",
            [typeof(AskJoobyAnalyticsInstanceVersion)] =
                "Audit history. A copy of the instance row taken on each update, like the other Version tables. The approval " +
                "history itself is carried and replays against the preserved current version; an entity whose current version " +
                "was awaiting approval at export time imports still awaiting approval, without the superseded row copies."
        };
    }
}