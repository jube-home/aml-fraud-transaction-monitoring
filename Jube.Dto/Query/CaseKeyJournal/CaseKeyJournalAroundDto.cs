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
    [Description("The other activity on the same day as one transaction, for each identifier it carries.")]
    public class CaseKeyJournalAroundDto
    {
        [Description("Start of the day (UTC) the transaction was created on.")]
        public DateTimeOffset From { get; set; }

        [Description("End of that day (UTC).")]
        public DateTimeOffset To { get; set; }

        [Description("One group for each identifier of the transaction that other transactions share that day. " +
                     "Identifiers nobody else used that day are left out.")]
        public List<CaseKeyJournalAroundGroupDto> Groups { get; set; } = [];
    }

    [Description("Same-day transactions that share one identifier with the transaction.")]
    public class CaseKeyJournalAroundGroupDto
    {
        [Description("Name of the identifier on the transaction, such as 'Device'.")]
        public string Name { get; set; } = string.Empty;

        [Description("The payload field the journal can be pivoted on to follow this identifier.")]
        public string Key { get; set; } = string.Empty;

        [Description("The identifier value the transactions share.")]
        public string Value { get; set; } = string.Empty;

        [Description("True when more transactions shared the identifier than are listed.")]
        public bool Truncated { get; set; }

        [Description("The other transactions, newest first. The transaction itself is not listed.")]
        public List<CaseKeyJournalAroundRowDto> Rows { get; set; } = [];
    }

    [Description("One same-day transaction.")]
    public class CaseKeyJournalAroundRowDto
    {
        public long Id { get; set; }
        public DateTimeOffset? ReferenceDate { get; set; }
        public double ResponseElevation { get; set; }
        public int ActivationCount { get; set; }

        [Description("Names of the activations the transaction raised, as far as the caller may see them.")]
        public string Activations { get; set; } = string.Empty;
    }
}
