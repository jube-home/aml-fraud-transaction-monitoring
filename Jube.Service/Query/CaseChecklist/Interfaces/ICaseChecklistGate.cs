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

using Jube.Service.Query.CaseChecklist.Models;

namespace Jube.Service.Query.CaseChecklist.Interfaces
{
    public interface ICaseChecklistGate
    {
        Task<IReadOnlyList<OpenChecklistItem>> EvaluateAsync(int caseId, string actor,
            CancellationToken token = default);

        Task<Jube.Data.Poco.CaseChecklistOverride?> FindLiveOverrideAsync(int caseId, Guid statusGuid, string mover,
            CancellationToken token = default);

        Task RecordBlockedAsync(int caseId, string actor, IReadOnlyList<OpenChecklistItem> open,
            CancellationToken token = default);

        Task ConsumeOverrideAsync(Jube.Data.Poco.CaseChecklistOverride grant, string mover,
            CancellationToken token = default);

        Task RecordBypassAsync(int caseId, string actor, CancellationToken token = default);
    }
}
