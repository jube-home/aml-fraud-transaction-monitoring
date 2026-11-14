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

namespace Jube.Engine.Erasure
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Query.EraseSubjectQuery;
    using Data.Query.GetErasureSubjectBlockersQuery;
    using Data.Repository;
    using Dto.Options;

    public sealed record ErasureRunResult(int Executed, int Refused);

    public static class ErasureExecutor
    {
        public static async Task<ErasureRunResult> RunAsync(DbContext dbContext, int retentionMinimumDays,
            DateTime now, CancellationToken token = default)
        {
            var executed = 0;
            var refused = 0;

            while (!token.IsCancellationRequested)
            {
                await using var transaction = await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);

                var repository = new ErasureRequestRepository(dbContext);
                var request = await repository.ClaimNextApprovedAsync(token).ConfigureAwait(false);

                if (request is null)
                {
                    await transaction.RollbackAsync(token).ConfigureAwait(false);

                    break;
                }

                await new EraseSubjectQuery(dbContext).LockCasesAsync(request.TenantRegistryId, request.KeyName,
                    request.KeyValue, token).ConfigureAwait(false);

                var blockers = await new GetErasureSubjectBlockersQuery(dbContext).ExecuteAsync(
                    request.TenantRegistryId, request.KeyName, request.KeyValue, retentionMinimumDays, now, token)
                    .ConfigureAwait(false);

                var refusal = blockers.Refusal();

                if (refusal is not null)
                {
                    await repository.CompleteAsync(request.Id, ErasureState.Refused, refusal,
                        (int)blockers.ArchiveRows, null, null, null, null, token).ConfigureAwait(false);
                    refused++;
                }
                else
                {
                    var result = await new EraseSubjectQuery(dbContext).ExecuteAsync(request.TenantRegistryId,
                        request.KeyName, request.KeyValue, token).ConfigureAwait(false);

                    await repository.CompleteAsync(request.Id, ErasureState.Executed, null, (int)blockers.ArchiveRows,
                        (int)result.ArchiveRowsRemoved, (int)result.GraphNodesRemoved, (int)result.GraphEdgesRemoved,
                        (int)result.CasePayloadsNulled, token).ConfigureAwait(false);
                    executed++;
                }

                await transaction.CommitAsync(token).ConfigureAwait(false);
            }

            return new ErasureRunResult(executed, refused);
        }
    }
}
