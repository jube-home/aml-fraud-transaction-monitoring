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

using Jube.Service.Agent.Models;
using Jube.Service.Query.CaseKeyJournal;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseKeyJournal(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseKeyJournalGet", OperationKind.Read, true, false,
                    "Returns the transactions of a case key scored by the workflow's model, newest first, in one " +
                    "lens: payload, rules and activations, features, models or sanctions.",
                    CaseKeyJournalService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalAnalyticsGet", OperationKind.Read, true, false,
                    "Analyses the history of a case key: a summary, which transactions stand out against the key's " +
                    "own normal, progression, distributions, velocity, gaps, first-seen values, counterparties, " +
                    "correlation and drift.",
                    CaseKeyJournalService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalTransactionGet", OperationKind.Read, true, false,
                    "Opens one archived transaction: the activations that fired, the features and sanction " +
                    "distances the rules saw, and every model's score, threshold, calibration, contributions and " +
                    "decision path.",
                    CaseKeyJournalService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalAroundGet", OperationKind.Read, true, false,
                    "Shows the other transactions on the same day that shared an account, device, address or " +
                    "counterparty with one transaction.",
                    CaseKeyJournalService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalTransactionPayloadGet", OperationKind.Read, true, false,
                    "Returns the payload of one archived transaction limited to the fields the caller's roles may " +
                    "see, and records that it was read.",
                    CaseKeyJournalService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalTagOptionsGet", OperationKind.Read, true, false,
                    "Lists the Tags the workflow's model allows, which are the only names that can be added to a " +
                    "journal transaction.",
                    CaseKeyJournalTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalTagUpdate", OperationKind.Write, true, false,
                    "Adds Tags to and removes Tags from up to 500 journal transactions in one step, returning the " +
                    "Tag sets before and after so that the step can be undone.",
                    CaseKeyJournalTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalTagJobPreview", OperationKind.Read, true, false,
                    "Counts how many transactions a journal view matches, so that tagging all of them can be " +
                    "confirmed first.",
                    CaseKeyJournalTagJobService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalTagJobSubmit", OperationKind.Write, false, false,
                    "Starts a background job that adds Tags to and removes Tags from every transaction a journal " +
                    "view matches.",
                    CaseKeyJournalTagJobService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalTagJobGet", OperationKind.Read, true, false,
                    "Returns a bulk tagging job with its state and how far it has got.",
                    CaseKeyJournalTagJobService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalTagJobList", OperationKind.Read, true, false,
                    "Lists the caller's recent bulk tagging jobs on a Case Workflow.",
                    CaseKeyJournalTagJobService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalTagJobCancel", OperationKind.Write, true, false,
                    "Asks a queued or running bulk tagging job to stop.",
                    CaseKeyJournalTagJobService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalTagJobUndo", OperationKind.Write, false, false,
                    "Starts a job that puts every transaction a finished bulk tagging changed back to the Tags it " +
                    "had.",
                    CaseKeyJournalTagJobService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalPinList", OperationKind.Read, true, false,
                    "Lists the transactions pinned in the journal of a case.",
                    CaseKeyJournalPinService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalPinSet", OperationKind.Write, true, false,
                    "Pins or unpins journal transactions on a case so they stay at the top of the journal for " +
                    "everyone working it.",
                    CaseKeyJournalPinService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalTrackGet", OperationKind.Read, true, false,
                    "Reads the transactions of a journal view in time order with their location, and works out the " +
                    "distance, hours and speed between consecutive transactions of each key, marking legs that " +
                    "could not have been travelled.",
                    CaseKeyJournalTrackService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalReplayList", OperationKind.Read, true, false,
                    "Lists the replays saved on a case: the settings that draw the movement of the case key on a " +
                    "map again.",
                    CaseKeyJournalReplayService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalReplaySave", OperationKind.Write, false, false,
                    "Saves a replay on a case: a name, the window in days, merged keys and a filter on the Archive.",
                    CaseKeyJournalReplayService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalReplayDelete", OperationKind.Write, true, false,
                    "Removes a saved replay from a case so that it is no longer offered.",
                    CaseKeyJournalReplayService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalReplaySummaryGet", OperationKind.Read, true, false,
                    "Works out a saved replay again from the archive and returns the movement it comes to, with the " +
                    "fastest legs that could not have been travelled.",
                    CaseKeyJournalReplayService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseKeyJournalTagRestore", OperationKind.Write, true, false,
                    "Puts journal transactions back to the Tag sets they had, which undoes a tagging.",
                    CaseKeyJournalTagService.RequiredPermissions)
            ]);
        }
    }
}
