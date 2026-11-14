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

using Jube.Data.Context;
using Jube.Engine.CaseEscalation;
using Jube.Service.Query.CaseChecklist.Interfaces;
using Jube.Service.Repository.CaseWorkflowFormValidator;
using log4net;

namespace Jube.Service.Query.CaseChecklist
{
    public sealed class ServiceCaseChecklistBypass(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log)
        : ICaseChecklistBypass
    {
        public Task RecordAsync(DbContext dbContext, int caseId, string actor, CancellationToken token = default)
        {
            var reconciler = new CaseChecklistReconciler(dbContext,
                FormValidatorRuntime.SqlRunner(dynamicEnvironment, actor, log), TimeProvider.System, log);

            ICaseChecklistGate gate = new CaseChecklistGate(dbContext, reconciler, () => dbContext, TimeProvider.System,
                log);

            return gate.RecordBypassAsync(caseId, actor, token);
        }
    }
}
