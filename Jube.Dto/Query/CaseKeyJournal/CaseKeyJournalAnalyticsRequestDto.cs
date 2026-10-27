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
    [Description("Which analytics of a case key's transactions to compute. The key, value, window, limit and " +
                 "response elevation filters are those of the journal; Limit is the most transactions used as the " +
                 "history, from 1 to 5000, default 2000, newest first.")]
    public class CaseKeyJournalAnalyticsRequestDto : CaseKeyJournalRequestDto
    {
        [Description("Comma separated sections to compute: summary, notable, progression, distributions, " +
                     "velocity, gaps, novelty, counterparties, correlation, drift, peers. Defaults to all.")]
        public string? Sections { get; set; }

        [Description("Comma separated measure field names to restrict the numeric analytics to. Defaults to " +
                     "every measure the caller may see, up to forty.")]
        public string? Fields { get; set; }

        [Description("Name of the measure field that holds the money of a transaction, so that counterparties " +
                     "can be totalled. Optional.")]
        public string? AmountField { get; set; }
    }
}
