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
using Jube.Data.Repository;
using Jube.Dto.Query.InboundQueue;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using LinqToDB.Async;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Service.Query.InboundQueue
{
    public sealed class InboundQueueAdminService
    {
        public const int ModelPermission = 6;
        public const int MaximumDead = 200;
        private static readonly int[] permissions = [ModelPermission];

        private readonly DbContext dbContext;
        private readonly string userName;
        private readonly int tenantRegistryId;
        private readonly PermissionValidation permissionValidation;
        private readonly ILog log;
        private readonly ILog auditLog;
        private readonly IServiceChangeBus serviceChangeBus;

        private InboundQueueAdminService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
        }

        public static async Task<InboundQueueAdminService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");
            }

            var tenant = await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, userName, token)
                .ConfigureAwait(false);

            if (tenant is null)
            {
                throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");
            }

            var validation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new InboundQueueAdminService(dbContext, userName, tenant.Value, validation, log,
                LogManager.GetLogger("Jube.Audit"), serviceChangeBus);
        }

        private void Ensure()
        {
            if (!permissionValidation.Validate(permissions))
            {
                throw new CaseExceptions.ForbiddenException("Permission denied.", permissions);
            }
        }

        public async Task<InboundQueueDepthDto> DepthAsync(CancellationToken token = default)
        {
            Ensure();

            var depth = await new TransactionInboundQueueRepository(dbContext).DepthForTenantAsync(tenantRegistryId, token).ConfigureAwait(false);

            return new InboundQueueDepthDto { Pending = depth.Pending, Processing = depth.Processing, Dead = depth.Dead };
        }

        public async Task<List<InboundQueueDeadDto>> ListDeadAsync(CancellationToken token = default)
        {
            Ensure();

            return
            [
                .. (await new TransactionInboundQueueRepository(dbContext)
                    .ListDeadForTenantAsync(tenantRegistryId, MaximumDead, token).ConfigureAwait(false))
                .Select(r => new InboundQueueDeadDto
                {
                    Id = r.Id,
                    EntityAnalysisModelGuid = r.EntityAnalysisModelGuid,
                    Attempts = r.Attempts,
                    LastError = r.LastError,
                    EnqueuedBy = r.EnqueuedBy,
                    CreatedDate = new DateTimeOffset(DateTime.SpecifyKind(r.CreatedDate, DateTimeKind.Utc))
                })
            ];
        }

        public Task RequeueAsync(long id, CancellationToken token = default) =>
            ActAsync("Requeue", id, (repository, row) => repository.RequeueDeadAsync(row, token), token);

        public Task DeleteAsync(long id, CancellationToken token = default) =>
            ActAsync("Delete", id, (repository, row) => repository.DeleteDeadAsync(row, token), token);

        private async Task ActAsync(string operation, long id,
            Func<TransactionInboundQueueRepository, long, Task<int>> action, CancellationToken token)
        {
            using var op = OperationScope.Start("InboundQueue", operation, userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                Ensure();

                var repository = new TransactionInboundQueueRepository(dbContext);
                var inTenant = await repository.DeadBelongsToTenantAsync(tenantRegistryId, id, token)
                    .ConfigureAwait(false);

                if (!inTenant || await action(repository, id).ConfigureAwait(false) == 0)
                {
                    throw new CaseExceptions.NotFoundException("The dead row was not found.");
                }

                op.Entity((int)id);
                op.Updated();
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }
    }
}
