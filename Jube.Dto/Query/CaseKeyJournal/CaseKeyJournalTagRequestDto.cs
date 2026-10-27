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
    [Description("Tags to add to and remove from a set of journal transactions in one step.")]
    public class CaseKeyJournalTagRequestDto
    {
        [Description("Guid of the Case Workflow. Only transactions scored by the workflow's model are changed.")]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("EntityAnalysisModelInstanceEntryGuid of each transaction to change, as returned in the " +
                     "journal. Between one and 500.")]
        public Guid[] Transactions { get; set; } = [];

        [Description("Tag names to add to every transaction. Each must be an active Tag of the workflow's model. " +
                     "A transaction that already carries a name is left as it is.")]
        public string[]? Add { get; set; }

        [Description("Tag names to remove from every transaction. A transaction that does not carry a name is " +
                     "left as it is.")]
        public string[]? Remove { get; set; }
    }
}
