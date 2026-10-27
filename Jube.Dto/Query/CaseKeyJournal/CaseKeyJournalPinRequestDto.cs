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
    [Description("Pins or unpins transactions in the journal of a Case.")]
    public class CaseKeyJournalPinRequestDto
    {
        [Description("Id of the Case the journal belongs to.")]
        public int CaseId { get; set; }

        [Description("EntityAnalysisModelInstanceEntryGuid of each transaction to pin or unpin, as returned in " +
                     "the journal. Between one and 50.")]
        public Guid[] Transactions { get; set; } = [];

        [Description("True to pin the transactions, false to unpin them.")]
        public bool Pinned { get; set; }
    }
}
