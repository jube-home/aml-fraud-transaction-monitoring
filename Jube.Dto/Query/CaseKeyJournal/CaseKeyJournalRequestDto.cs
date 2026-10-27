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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseKeyJournal
{
    [Description("Which transactions of a case key to return, in which lens, over what window.")]
    public class CaseKeyJournalRequestDto
    {
        [Description("Guid of the Case Workflow. The journal covers the transactions scored by the model the " +
                     "workflow belongs to, and the payload columns are the XPaths the caller's roles may see.")]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("Payload field to match, normally the case key such as 'AccountId', or any identifier to " +
                     "pivot the journal to.")]
        public string? Key { get; set; }

        [Description("Value the payload field must equal.")]
        public string? Value { get; set; }

        [Description("Which column set to return: payload, rules, features, models or sanctions. Defaults to " +
                     "payload. The response always lists how many transactions each lens has something for.")]
        public string? Lens { get; set; }

        [Description("Start of the window (UTC). Defaults to ninety days before the end.")]
        public DateTimeOffset? From { get; set; }

        [Description("End of the window (UTC). Defaults to now.")]
        public DateTimeOffset? To { get; set; }

        [Description("Most transactions to return, newest first, from 1 to 5000. Defaults to 500.")]
        public int? Limit { get; set; }

        [Description("When true only transactions that raised at least one activation are returned.")]
        public bool? ActivationsOnly { get; set; }

        [Description("Least response elevation a transaction must have. Defaults to 0.")]
        public double? ResponseElevation { get; set; }

        [Description("Id of the Case the journal is being read for. When given, the transactions pinned on that " +
                     "case are returned first in every window, marked Pinned.")]
        public int? CaseId { get; set; }

        [Description("When true, with a CaseId, only the transactions pinned on that case are returned, whatever " +
                     "their key or date.")]
        public bool? PinnedOnly { get; set; }

        [Description("Up to three more keys to read on the same timeline, as a JSON array of objects with a key " +
                     "(payload field) and a value, for example [{\"key\":\"Device\",\"value\":\"D-9\"}]. The case " +
                     "key is lane A and these are lanes B to D; each row says which lanes it belongs to.")]
        public string? Merge { get; set; }

        [Description("The filter on the Archive, as a JSON object of condition (AND or OR) and rules, each rule " +
                     "an id (a field of the model such as Payload.Amount), an operator (equal, not_equal, less, " +
                     "less_or_equal, greater, greater_or_equal, like, not_like) and a value, or a nested group. " +
                     "Only transactions matching it are returned. Leave empty for no filter.")]
        public string? Filter { get; set; }
    }
}
