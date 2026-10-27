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
using Jube.Dto.Hold;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseHold
{
    public class CaseHoldDto
    {
        [Description("Identifier of the archived transaction that carries the hold, matching " +
                     "EntityAnalysisModelInstanceEntryGuid on the Archive payload.")]
        public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }

        [Description("The value of the transaction's entry key, for example the payment reference, when it has one.")]
        public string? EntryKeyValue { get; set; }

        [Description("When the transaction took place, from its reference date.")]
        public DateTimeOffset? ReferenceDate { get; set; }

        [Description("The state of the hold: 1 Held, 2 Declined, 3 Released, 4 Refused. Only a Held payment can " +
                     "be released or refused.")]
        public int StateId { get; set; }

        [Description("The name of the response elevation range that held or declined the transaction.")]
        public string? RangeName { get; set; }

        [Description("The lowest response elevation of the range that held or declined the transaction.")]
        public int RangeMinimum { get; set; }

        [Description("The highest response elevation of the range that held or declined the transaction.")]
        public int RangeMaximum { get; set; }

        [Description("The response elevation the transaction was given.")]
        public int Elevation { get; set; }

        [Description("When the hold was stamped on the transaction.")]
        public DateTimeOffset StampedDate { get; set; }

        [Description("When the payment hold diary of the case falls due, when the case has one; the time by which " +
                     "the payment should be released or refused.")]
        public DateTimeOffset? DeadlineDate { get; set; }

        [Description("Who released or refused the payment, when, why and through which release hook; null " +
                     "while the payment is still held or was declined.")]
        public ResponseHoldDecision? Decision { get; set; }

        [Description("The messages the response elevation range and the release hook sent about the hold, each " +
                     "with its outcome.")]
        public List<ResponseHoldLeg> Legs { get; set; } = [];

        [Description("Name of the release hook that would release this payment, or null when no active release " +
                     "hook of the Case Workflow handles it.")]
        public string? ReleaseHookName { get; set; }

        [Description("True when the release hook has a refuse URL, so that refusing also calls the payment system.")]
        public bool RefuseCallsHook { get; set; }
    }
}
