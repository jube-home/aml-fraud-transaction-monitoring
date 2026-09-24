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

using System.ComponentModel;
using Jube.Dto.Validation;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.Query.EntityAnalysisModelHistorySimulation
{
    [Description("The value an abstraction rule would have for the transaction in a context, worked out as the " +
                 "engine does: the transaction history for the search key value is fetched from the cache, " +
                 "filtered by the rule, limited to the window, and aggregated. Nothing is stored.")]
    public class AbstractionSimulationResultDto
    {
        [Description("True when the rule compiled and the simulation ran; when false, Errors says why.")]
        public bool Simulated { get; set; }

        [Description("Why the simulation did not run, e.g. the rule did not compile, the search key has no value " +
                     "in the context, or the cache is unavailable.")]
        public List<ValidationErrorDto> Errors { get; set; } = [];

        [Description("True when the search key is cache backed, so the engine reads a value computed in the " +
                     "background rather than aggregating history; Value is then that cached value.")]
        public bool CacheBacked { get; set; }

        [Description("The search key field, e.g. AccountId.")]
        public string SearchKey { get; set; } = string.Empty;

        [Description("The search key value taken from the context, e.g. the account id.")]
        public string? SearchKeyValue { get; set; }

        [Description("How many history entries the engine may fetch: the lower of the model's cache fetch limit " +
                     "and the search key's fetch limit.")]
        public int FetchLimit { get; set; }

        [Description("History entries fetched from the cache for the search key value.")]
        public int DocumentsFetched { get; set; }

        [Description("True when the fetch limit was reached, so older history the window might include was not " +
                     "considered, exactly as in the engine.")]
        public bool FetchLimitReached { get; set; }

        [Description("Documents the rule was evaluated against: the fetched history plus the current transaction.")]
        public int DocumentsEvaluated { get; set; }

        [Description("Documents the rule matched, before the window was applied.")]
        public int Matched { get; set; }

        [Description("Matched documents inside the window; these are what is aggregated.")]
        public int InWindow { get; set; }

        [Description("Start of the window, in UTC.")]
        public DateTime? WindowFrom { get; set; }

        [Description("End of the window (the reference date), in UTC.")]
        public DateTime? WindowTo { get; set; }

        [Description("True when the search key's TTL is shorter than the rule's interval, so the engine uses the " +
                     "shorter window without saying so.")]
        public bool WindowShortenedBySearchKey { get; set; }

        [Description("The abstraction value the engine would produce, as invariant text; null when not simulated.")]
        public string? Value { get; set; }

        [Description("Up to 20 matched documents inside the window, as field name to invariant text, for checking " +
                     "what was counted.")]
        public List<Dictionary<string, string?>> Sample { get; set; } = [];

        [Description("The error the rule raised while filtering the history, when it did; otherwise null.")]
        public string? RuntimeError { get; set; }
    }
}