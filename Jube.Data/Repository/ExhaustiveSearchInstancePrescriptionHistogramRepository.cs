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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Poco;

    public class ExhaustiveSearchInstancePrescriptionHistogramRepository(DbContext dbContext)
    {
        public async Task<ExhaustiveSearchInstancePrescriptionHistogram> InsertAsync(
            ExhaustiveSearchInstancePrescriptionHistogram model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task<IEnumerable<ExhaustiveSearchInstancePrescriptionHistogram>>
            GetByPromotedTrialInstanceVariableIdOrderByIdAsync(
                int exhaustiveSearchInstancePromotedTrialInstanceVariableId, CancellationToken token = default)
        {
            return await dbContext.ExhaustiveSearchInstancePrescriptionHistogram
                .Where(w => w.ExhaustiveSearchInstancePromotedTrialInstanceVariableId ==
                            exhaustiveSearchInstancePromotedTrialInstanceVariableId)
                .OrderBy(o => o.Id).ToListAsync(token).ConfigureAwait(false);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance,
            CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstancePrescriptionHistogram
                .Where(d => d.ExhaustiveSearchInstancePromotedTrialInstanceVariable
                                .ExhaustiveSearchInstanceTrialInstanceVariable.ExhaustiveSearchInstanceTrialInstance
                                .ExhaustiveSearchInstance.EntityAnalysisModel.TenantRegistryId ==
                            tenantRegistryIdOutsideOfInstance)
                .DeleteAsync(token);
        }
    }
}