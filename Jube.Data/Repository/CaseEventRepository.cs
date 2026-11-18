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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;
    using LinqToDB.Data;
    using Poco;
    using Query.MarkCaseChecklistStaleQuery;

    public class CaseEventRepository
    {
        public const int FirstChecklistEventTypeId = 21;
        public const int LastChecklistEventTypeId = 28;
        public const int TriageRecommendedEventTypeId = 29;
        public const int TriageAcceptedEventTypeId = 30;
        public const int TriageOverriddenEventTypeId = 31;
        public const int LegalHoldPlacedEventTypeId = 32;
        public const int LegalHoldLiftedEventTypeId = 33;
        public const int AnalystAssessmentRecordedEventTypeId = 34;
        public const int AnalystAssessmentWithdrawnEventTypeId = 35;
        public const int CaseLinkedEventTypeId = 36;
        public const int CaseUnlinkedEventTypeId = 37;

        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public CaseEventRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;

            tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseEventRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public CaseEventRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IEnumerable<CaseEvent>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.CaseEvent.Where(w =>
                tenantRegistryId != null &&
                w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId).ToListAsync(token);
        }

        public Task<CaseEvent> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.CaseEvent.FirstOrDefaultAsync(w
                => tenantRegistryId != null
                   && w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                   && w.Id == id, token);
        }

        public async Task<CaseEvent> InsertAsync(CaseEvent model, CancellationToken token = default)
        {
            CaseEventValueGuard.EnsureCanonical(model);

            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            await MarkStaleAsync([model], token).ConfigureAwait(false);

            return model;
        }

        public async Task BulkInsertAsync(IEnumerable<CaseEvent> models, CancellationToken token = default)
        {
            var list = models as IReadOnlyCollection<CaseEvent> ?? models.ToList();

            foreach (var model in list)
            {
                CaseEventValueGuard.EnsureCanonical(model);
            }

            await dbContext.BulkCopyAsync(list, token).ConfigureAwait(false);

            await MarkStaleAsync(list, token).ConfigureAwait(false);
        }

        public static bool IsChecklistEventType(int? caseEventTypeId)
        {
            return caseEventTypeId is >= FirstChecklistEventTypeId and <= LastChecklistEventTypeId;
        }

        private Task MarkStaleAsync(IEnumerable<CaseEvent> models, CancellationToken token)
        {
            var caseIds = models.Where(w => !IsChecklistEventType(w.CaseEventTypeId)).Select(s => s.CaseId)
                .Where(w => w.HasValue).Select(s => s!.Value).Distinct().ToList();

            return caseIds.Count == 0
                ? Task.CompletedTask
                : new MarkCaseChecklistStaleQuery(dbContext).ExecuteAsync(caseIds, token);
        }
    }
}