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
    [Description("A view of the Case Key Journal whose matching transactions are to be counted or tagged.")]
    public class CaseKeyJournalTagJobPreviewRequestDto
    {
        [Description("Guid of the Case Workflow whose model scored the transactions.")]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("Payload field of the view, normally the case key such as 'AccountId', or the identifier " +
                     "pivoted to.")]
        public string? Key { get; set; }

        [Description("Value the payload field must equal.")]
        public string? Value { get; set; }

        [Description("Start of the window (UTC). Defaults to ninety days before the end.")]
        public DateTimeOffset? From { get; set; }

        [Description("End of the window (UTC). Defaults to now.")]
        public DateTimeOffset? To { get; set; }

        [Description("When true only transactions that raised at least one activation match.")]
        public bool? ActivationsOnly { get; set; }

        [Description("Least response elevation a transaction must have. Defaults to 0.")]
        public double? ResponseElevation { get; set; }

        [Description("Up to three more keys whose transactions are part of the view, as a JSON array of objects " +
                     "with a key and a value, for example [{\"key\":\"Device\",\"value\":\"D-9\"}].")]
        public string? Merge { get; set; }

        [Description("The filter on the Archive, as a JSON object of condition (AND or OR) and rules, each rule " +
                     "an id (a field of the model such as Payload.Amount), an operator (equal, not_equal, less, " +
                     "less_or_equal, greater, greater_or_equal, like, not_like) and a value, or a nested group. " +
                     "Only transactions matching it are counted or tagged. Leave empty for no filter.")]
        public string? Filter { get; set; }
    }
}
