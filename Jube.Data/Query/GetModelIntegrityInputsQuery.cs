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

namespace Jube.Data.Query
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Repository;
    using Models;

    public class GetModelIntegrityInputsQuery(DbContext dbContext, int tenantRegistryId)
    {
        public async Task<ModelIntegrityInputsDto> ExecuteAsync(int entityAnalysisModelId,
            CancellationToken token = default)
        {
            var dto = new ModelIntegrityInputsDto();

            foreach (var g in await new EntityAnalysisModelGatewayRuleRepository(dbContext, tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
            {
                dto.CompileStatuses.Add(new ModelIntegrityCompileStatus("GatewayRule", g.Id, g.Name, g.Active == 1,
                    ToCompiled(g.Compiled), g.CompileError));
            }

            var xPaths = (await new EntityAnalysisModelRequestXPathRepository(dbContext, tenantRegistryId)
                .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false)).ToList();

            foreach (var a in await new EntityAnalysisModelAbstractionRuleRepository(dbContext, tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModelId, token)
                         .ConfigureAwait(false))
            {
                dto.CompileStatuses.Add(new ModelIntegrityCompileStatus("AbstractionRule", a.Id, a.Name, a.Active == 1,
                    ToCompiled(a.Compiled), a.CompileError));

                if (a.Active != 1 || a.Search != 1)
                {
                    continue;
                }

                var searchKey = xPaths.FirstOrDefault(x => x.Name == a.SearchKey && x.Active == 1);
                if (searchKey != null)
                {
                    dto.SearchRules.Add(new ModelIntegritySearchRule(a.Id, a.Name, a.SearchInterval ?? "d",
                        a.SearchInterval != null ? a.SearchValue : 0, a.SearchKey,
                        searchKey.SearchKeyTtlInterval ?? "d", searchKey.SearchKeyTtlIntervalValue ?? 0));
                }
            }

            foreach (var c in await new EntityAnalysisModelAbstractionCalculationRepository(dbContext,
                             tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModelId, token)
                         .ConfigureAwait(false))
            {
                dto.CompileStatuses.Add(new ModelIntegrityCompileStatus("AbstractionCalculation", c.Id, c.Name,
                    c.Active == 1,
                    ToCompiled(c.Compiled), c.CompileError));
            }

            foreach (var r in await new EntityAnalysisModelActivationRuleRepository(dbContext, tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModelId, token)
                         .ConfigureAwait(false))
            {
                dto.CompileStatuses.Add(new ModelIntegrityCompileStatus("ActivationRule", r.Id, r.Name, r.Active == 1,
                    ToCompiled(r.Compiled), r.CompileError));
            }

            dto.Nodes = await dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry
                .Where(w => w.TenantRegistryId == tenantRegistryId)
                .OrderBy(o => o.Instance)
                .Select(s => new ModelIntegrityNode(s.Instance, s.HeartbeatDate, s.SynchronisedDate))
                .ToListAsync(token)
                .ConfigureAwait(false);

            return dto;
        }

        private static bool? ToCompiled(byte? compiled)
        {
            return compiled switch
            {
                null => null,
                0 => false,
                _ => true
            };
        }
    }
}