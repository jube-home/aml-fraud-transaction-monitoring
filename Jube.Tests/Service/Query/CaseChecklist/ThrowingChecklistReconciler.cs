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

using System;
using System.Threading;
using System.Threading.Tasks;
using Jube.Service.Query.CaseChecklist.Interfaces;

namespace Jube.Test.Service.Query.CaseChecklist
{
    public sealed class ThrowingChecklistReconciler : ICaseChecklistReconciler
    {
        public Task<int> ReconcileAsync(int caseId, string actor, string trigger, CancellationToken token = default)
        {
            throw new InvalidOperationException("The checklist could not be reconciled.");
        }

        public Task<int> ReconcileIfNeededAsync(int caseId, string actor, string trigger, TimeSpan maxAge,
            CancellationToken token = default)
        {
            throw new InvalidOperationException("The checklist could not be reconciled.");
        }
    }
}
