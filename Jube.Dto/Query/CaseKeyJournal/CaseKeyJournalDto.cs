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
    [Description("One column of the journal.")]
    public class CaseKeyJournalColumnDto
    {
        [Description("Key of the column in every row. Lens columns are 'group:name', such as 'abstraction:Volume'.")]
        public string Name { get; set; } = string.Empty;

        [Description("Text to show for the column.")]
        public string Title { get; set; } = string.Empty;

        [Description("number, string, date, boolean or array.")]
        public string Type { get; set; } = "string";

        [Description("What kind of field this is: 1 Measure, 2 Category, 3 Counterparty, 4 Identifier, 5 Time, " +
                     "6 Geography. Configured on the Case Workflow XPath, otherwise inferred from the values.")]
        public int RoleId { get; set; }

        [Description("True when the role was inferred rather than configured.")]
        public bool RoleInferred { get; set; }

        [Description("The lens the column belongs to, or 'base' for the columns every lens carries.")]
        public string Lens { get; set; } = "base";

        [Description("Facet the column comes from: payload, rule, abstraction, calculation, counter, dictionary, " +
                     "exhaustive, advanced, sanction or http.")]
        public string Group { get; set; } = "payload";

        [Description("True when the field can be pivoted to: the journal can be re-run for any value in it.")]
        public bool Drill { get; set; }
    }

    [Description("How many returned transactions have something to show in a lens.")]
    public class CaseKeyJournalLensDto
    {
        [Description("payload, rules, features, models or sanctions.")]
        public string Lens { get; set; } = string.Empty;

        [Description("Transactions with something in this lens.")]
        public int Rows { get; set; }
    }

    [Description("The columns that locate a transaction.")]
    public class CaseKeyJournalLocationDto
    {
        public string Latitude { get; set; } = string.Empty;
        public string Longitude { get; set; } = string.Empty;
    }

    [Description("A payload field the journal can be pivoted to or merged by.")]
    public class CaseKeyJournalDrillKeyDto
    {
        [Description("The payload field to use as a key.")]
        public string Key { get; set; } = string.Empty;

        [Description("Text to show for the key.")]
        public string Title { get; set; } = string.Empty;
    }

    [Description("What was asked and what was found.")]
    public class CaseKeyJournalMetaDto
    {
        [Description("The payload fields the caller's roles can see that are set as drill fields, so a key can be " +
                     "chosen to pivot or merge by.")]
        public List<CaseKeyJournalDrillKeyDto> DrillKeys { get; set; } = [];

        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public Guid CaseWorkflowGuid { get; set; }
        public string Lens { get; set; } = "payload";
        public int RowCount { get; set; }
        public int Limit { get; set; }

        [Description("True when more transactions matched than the limit allowed.")]
        public bool Truncated { get; set; }

        [Description("How many transactions the key has in the window before the filter, up to 100,000. Equals " +
                     "RowCount when no filter was given.")]
        public int Total { get; set; }

        [Description("The names of the payload columns holding the latitude and the longitude of a transaction, as " +
                     "the model resolves its location, when the caller's roles can see both. Null otherwise, in " +
                     "which case the journal cannot be drawn on a map.")]
        public CaseKeyJournalLocationDto? Location { get; set; }

        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }
    }

    [Description("The Case Key Journal: the transactions for a key, with the columns of one lens. Read-only.")]
    public class CaseKeyJournalDto
    {
        public CaseKeyJournalMetaDto Meta { get; set; } = new();

        [Description("The columns of this lens, preceded by the base columns.")]
        public List<CaseKeyJournalColumnDto> Columns { get; set; } = [];

        [Description("Which lenses have anything to show for these transactions.")]
        public List<CaseKeyJournalLensDto> Lenses { get; set; } = [];

        [Description("Rows newest first. Each is a column name to value map, with optional 'CellFormat' and " +
                     "'BoldLine' entries carrying the conditional formatting of the payload lens.")]
        public List<Dictionary<string, object?>> Rows { get; set; } = [];
    }
}
