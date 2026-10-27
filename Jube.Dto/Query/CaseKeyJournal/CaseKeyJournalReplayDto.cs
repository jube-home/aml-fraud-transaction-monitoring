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
    [Description("A replay saved on a case: the settings to draw the movement of the case key on a map again.")]
    public class CaseKeyJournalReplayDto
    {
        public Guid Guid { get; set; }
        public int CaseId { get; set; }
        public string Name { get; set; } = string.Empty;

        [Description("How many days back from now the replay looks: 7, 30, 90 or 365.")]
        public int Days { get; set; }

        [Description("The other keys drawn as trails of their own, a JSON array of objects with a key and a value, " +
                     "or empty.")]
        public string? Merge { get; set; }

        [Description("The filter on the Archive, a JSON object of a condition (AND or OR) and rules, each a field of the model, an operator and a value, or empty.")]
        public string? Filter { get; set; }

        [Description("The speed in kilometres an hour above which a leg is impossible, or empty for the default of " +
                     "900.")]
        public double? LimitKmh { get; set; }

        [Description("The shortest leg in kilometres that can be impossible, or empty for the default of 50.")]
        public double? MinimumKilometres { get; set; }

        [Description("True when the replay shows only the legs that could not have been travelled, with the " +
                     "transactions at either end.")]
        public bool ImpossibleOnly { get; set; }

        [Description("True when every transaction of the view is drawn with the filtered ones picked out.")]
        public bool ShowAll { get; set; }

        [Description("The journal lens the view was on: payload, rules, features, models or sanctions. Empty means " +
                     "payload.")]
        public string? Lens { get; set; }

        public string CreatedUser { get; set; } = string.Empty;
        public DateTimeOffset CreatedDate { get; set; }
    }

    [Description("The views of the case journal saved on a case.")]
    public class CaseKeyJournalReplaysDto
    {
        public List<CaseKeyJournalReplayDto> Replays { get; set; } = [];
    }

    [Description("A saved view of the case journal to store on a case.")]
    public class CaseKeyJournalReplayRequestDto
    {
        [Description("Id of the case.")]
        public int CaseId { get; set; }

        [Description("A name of 1 to 100 characters.")]
        public string? Name { get; set; }

        [Description("How many days back from now to look: 7, 30, 90 or 365.")]
        public int Days { get; set; }

        [Description("Up to three more keys drawn as their own trails, a JSON array of objects with a key and a " +
                     "value.")]
        public string? Merge { get; set; }

        [Description("The filter on the Archive in force, a JSON object of a condition (AND or OR) and rules, each a field of the model, an operator and a value.")]
        public string? Filter { get; set; }

        [Description("The speed in kilometres an hour above which a leg is impossible. Between 1 and 10,000. " +
                     "Defaults to 900.")]
        public double? LimitKmh { get; set; }

        [Description("The shortest leg in kilometres that can be impossible. Between 0 and 20,000. Defaults to " +
                     "50.")]
        public double? MinimumKilometres { get; set; }

        [Description("True to show only the legs that could not have been travelled and the transactions at either " +
                     "end of them.")]
        public bool? ImpossibleOnly { get; set; }

        [Description("True to draw every transaction of the view with the filtered ones picked out.")]
        public bool? ShowAll { get; set; }

        [Description("The journal lens the view is on: payload, rules, features, models or sanctions. Empty means " +
                     "payload.")]
        public string? Lens { get; set; }
    }
}
