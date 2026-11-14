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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseKeyJournal
{
    [Description("The other activity on the same day as one transaction, for each identifier it carries.")]
    [MessagePackObject]
    public class CaseKeyJournalAroundDto
    {
        [Description("Start of the day (UTC) the transaction was created on.")]
        [Key(0)]
        public DateTimeOffset From { get; set; }

        [Description("End of that day (UTC).")]
        [Key(1)]
        public DateTimeOffset To { get; set; }

        [Description("One group for each identifier of the transaction that other transactions share that day. " +
                     "Identifiers nobody else used that day are left out.")]
        [Key(2)]
        public List<CaseKeyJournalAroundGroupDto> Groups { get; set; } = [];
    }

    [Description("Same-day transactions that share one identifier with the transaction.")]
    [MessagePackObject]
    public class CaseKeyJournalAroundGroupDto
    {
        [Description("Name of the identifier on the transaction, such as 'Device'.")]
        [Key(0)]
        public string Name { get; set; } = string.Empty;

        [Description("The payload field the journal can be pivoted on to follow this identifier.")]
        [Key(1)]
        public string Key { get; set; } = string.Empty;

        [Description("The identifier value the transactions share.")]
        [Key(2)]
        public string Value { get; set; } = string.Empty;

        [Description("True when more transactions shared the identifier than are listed.")]
        [Key(3)]
        public bool Truncated { get; set; }

        [Description("The other transactions, newest first. The transaction itself is not listed.")]
        [Key(4)]
        public List<CaseKeyJournalAroundRowDto> Rows { get; set; } = [];
    }

    [Description("One same-day transaction.")]
    [MessagePackObject]
    public class CaseKeyJournalAroundRowDto
    {
        [Key(0)]
        public long Id { get; set; }
        [Key(1)]
        public DateTimeOffset? ReferenceDate { get; set; }
        [Key(2)]
        public double ResponseElevation { get; set; }
        [Key(3)]
        public int ActivationCount { get; set; }

        [Description("Names of the activations the transaction raised, as far as the caller may see them.")]
        [Key(4)]
        public string Activations { get; set; } = string.Empty;
    }
}
